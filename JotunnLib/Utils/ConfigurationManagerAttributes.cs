using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Jotunn.Managers;
using UnityEngine;

/// <summary>
///     Class that specifies how a setting should be displayed inside the ConfigurationManager settings window.
///     
///     Usage:
///     This class template has to be copied inside the plugin's project and referenced by its code directly.
///     make a new instance, assign any fields that you want to override, and pass it as a tag for your setting.
///     
///     If a field is null (default), it will be ignored and won't change how the setting is displayed.
///     If a field is non-null (you assigned a value to it), it will override default behavior.
/// </summary>
/// 
/// <example> 
///     Here's an example of overriding order of settings and marking one of the settings as advanced:
///     <code>
///         // Override IsAdvanced and Order
///         Config.Bind("X", "1", 1, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 3 }));
///         // Override only Order, IsAdvanced stays as the default value assigned by ConfigManager
///         Config.Bind("X", "2", 2, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 1 }));
///         Config.Bind("X", "3", 3, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 2 }));
///     </code>
/// </example>
/// 
/// <remarks> 
///     You can read more and see examples in the readme at https://github.com/BepInEx/BepInEx.ConfigurationManager
///     You can optionally remove fields that you won't use from this class, it's the same as leaving them null.
/// </remarks>
public sealed class ConfigurationManagerAttributes
{
    /// <summary>
    ///     ctor
    /// </summary>
    public ConfigurationManagerAttributes()
    {
        EntryColor = new Color(1f, 0.631f, 0.235f, 1f); // Valheim orange
        DescriptionColor = Color.white;
    }

    /// <summary>
    ///     Should the setting be shown as a percentage (only use with value range settings).
    /// </summary>
    public bool? ShowRangeAsPercent;

    /// <summary>
    ///     Custom setting editor (OnGUI code that replaces the default editor provided by ConfigurationManager).
    ///     See below for a deeper explanation. Using a custom drawer will cause many of the other fields to do nothing.
    /// </summary>
    public Action<BepInEx.Configuration.ConfigEntryBase> CustomDrawer;

    /// <summary>
    ///     Show this setting in the settings screen at all? If false, don't show.
    /// </summary>
    public bool? Browsable;

    /// <summary>
    ///     Category the setting is under. Null to be directly under the plugin.
    /// </summary>
    public string Category;

    /// <summary>
    ///     If set, a "Default" button will be shown next to the setting to allow resetting to default.
    /// </summary>
    public object DefaultValue;

    /// <summary>
    ///     Force the "Reset" button to not be displayed, even if a valid DefaultValue is available. 
    /// </summary>
    public bool? HideDefaultButton;

    /// <summary>
    ///     Force the setting name to not be displayed. Should only be used with a <see cref="CustomDrawer"/> to get more space.
    ///     Can be used together with <see cref="HideDefaultButton"/> to gain even more space.
    /// </summary>
    public bool? HideSettingName;

    /// <summary>
    ///     Optional description shown when hovering over the setting.
    ///     Not recommended, provide the description when creating the setting instead.
    /// </summary>
    public string Description;

    /// <summary>
    ///     Name of the setting.
    /// </summary>
    public string DispName;

    /// <summary>
    ///     Order of the setting on the settings list relative to other settings in a category.
    ///     0 by default, higher number is higher on the list.
    /// </summary>
    public int? Order;

    /// <summary>
    ///     Only show the value, don't allow editing it.
    /// </summary>
    public bool? ReadOnly;

    /// <summary>
    ///     If true, don't show the setting by default. User has to turn on showing advanced settings or search for it.
    /// </summary>
    public bool? IsAdvanced;

    /// <summary>
    ///     Custom converter from setting type to string for the built-in editor textboxes.
    /// </summary>
    public System.Func<object, string> ObjToStr;

    /// <summary>
    ///     Custom converter from string to setting type for the built-in editor textboxes.
    /// </summary>
    public System.Func<string, object> StrToObj;

    /// <summary>
    ///     Whether a config is only writable by admins and gets overwritten on connecting clients
    /// </summary>
    public bool IsAdminOnly
    {
        get => isAdminOnly;
        set
        {
            isAdminOnly = value;
            bool locked = isAdminOnly && !SynchronizationManager.Instance.PlayerIsAdmin;
            IsUnlocked = !locked;
        }
    }

    private bool isAdminOnly;

    /// <summary>
    ///     Color of the entry text
    /// </summary>
    public Color EntryColor { get; set; }

    /// <summary>
    ///     Color of the description text
    /// </summary>
    public Color DescriptionColor { get; set; }

    /// <summary>
    ///     Whether a config is locked for direct writing
    /// </summary>
    public bool IsUnlocked
    {
        get => isUnlocked;
        internal set
        {
            ReadOnly = !value;
            HideDefaultButton = !value;
            isUnlocked = value;
        }
    }

    internal object autoCompleteList;

    private bool isUnlocked;

    private static readonly PropertyInfo[] _myProperties = typeof(ConfigurationManagerAttributes).GetProperties(BindingFlags.Instance | BindingFlags.Public);
    private static readonly FieldInfo[] _myFields = typeof(ConfigurationManagerAttributes).GetFields(BindingFlags.Instance | BindingFlags.Public);

    /// <summary>
    ///     Set config values from an attribute array
    /// </summary>
    /// <param name="attribs">Array of attribute values</param>
    public void SetFromAttributes(object[] attribs)
    {
        if (attribs == null || attribs.Length == 0)
        {
            return;
        }

        foreach (var attrib in attribs)
        {
            switch (attrib)
            {
                case null: break;

                case DisplayNameAttribute da:
                    DispName = da.DisplayName;
                    break;
                case CategoryAttribute ca:
                    Category = ca.Category;
                    break;
                case DescriptionAttribute de:
                    Description = de.Description;
                    break;
                case DefaultValueAttribute def:
                    DefaultValue = def.Value;
                    break;
                case ReadOnlyAttribute ro:
                    ReadOnly = ro.IsReadOnly;
                    break;
                case BrowsableAttribute bro:
                    Browsable = bro.Browsable;
                    break;

                // case Action<SettingEntryBase> newCustomDraw:
                //     CustomDrawer = _ => newCustomDraw(this);
                //     break;

                case string str:
                    switch (str)
                    {
                        case "ReadOnly":
                            ReadOnly = true;
                            break;
                        case "Browsable":
                            Browsable = true;
                            break;
                        case "Unbrowsable":
                        case "Hidden":
                            Browsable = false;
                            break;
                        case "Advanced":
                            IsAdvanced = true;
                            break;
                    }

                    break;

                // Copy attributes from a specially formatted object, currently recommended
                default:
                    var attrType = attrib.GetType();
                    if (attrType.Name == "ConfigurationManagerAttributes")
                    {
                        var otherFields = attrType.GetFields(BindingFlags.Instance | BindingFlags.Public);
                        foreach (var propertyPair in _myProperties.Join(otherFields, my => my.Name, other => other.Name, (my, other) => new { my, other }))
                        {
                            try
                            {
                                var val = propertyPair.other.GetValue(attrib);
                                if (val != null)
                                {
                                    // Handle delegate covariance not working when using reflection by manually converting the delegate
                                    if (propertyPair.my.PropertyType != propertyPair.other.FieldType && typeof(Delegate).IsAssignableFrom(propertyPair.my.PropertyType))
                                        val = Delegate.CreateDelegate(propertyPair.my.PropertyType, ((Delegate)val).Target, ((Delegate)val).Method);

                                    propertyPair.my.SetValue(this, val, null);
                                }
                            }
                            catch (Exception ex)
                            {
                                Jotunn.Logger.LogWarning($"Failed to copy value {propertyPair.my.Name} from provided tag object {attrType.FullName} - " + ex.Message);
                            }
                        }

                        // this is not done in the ConfigurationManager, is it even needed?
                        // https://github.com/BepInEx/BepInEx.ConfigurationManager/blob/master/ConfigurationManager/SettingEntryBase.cs
                        foreach (var fieldPair in _myFields.Join(otherFields, my => my.Name, other => other.Name, (my, other) => new { my, other }))
                        {
                            try
                            {
                                var val = fieldPair.other.GetValue(attrib);
                                if (val != null)
                                {
                                    // Handle delegate covariance not working when using reflection by manually converting the delegate
                                    if (fieldPair.my.FieldType != fieldPair.other.FieldType && typeof(Delegate).IsAssignableFrom(fieldPair.my.FieldType))
                                        val = Delegate.CreateDelegate(fieldPair.my.FieldType, ((Delegate)val).Target, ((Delegate)val).Method);

                                    fieldPair.my.SetValue(this, val);
                                }
                            }
                            catch (Exception ex)
                            {
                                Jotunn.Logger.LogWarning($"Failed to copy value {fieldPair.my.Name} from provided tag object {attrType.FullName} - " + ex.Message);
                            }
                        }

                        break;
                    }

                    return;
            }
        }
    }
}
