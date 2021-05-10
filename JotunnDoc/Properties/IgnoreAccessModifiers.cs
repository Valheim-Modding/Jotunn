using System.Security.Permissions;

// SecurityPermision set to minimum
// for skipping access modifiers check from the mono JIT
// The same attribute are added to the assembly when ticking
// Unsafe Code in the Project settings
// This is done here to allow an explanation of the trick and
// not in an outside source you could potentially miss.

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
