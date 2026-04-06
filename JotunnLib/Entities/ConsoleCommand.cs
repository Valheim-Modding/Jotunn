using System.Collections.Generic;

namespace Jotunn.Entities
{
    /// <summary>
    ///     A custom console command.
    /// </summary>
    public abstract class ConsoleCommand : CustomEntity
    {
        /// <summary>
        ///     The command that the user will need to type in their console to run your command.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        ///     The help text that will be displayed to the user for your command when they type `help` into their console.
        /// </summary>
        public abstract string Help { get; }

        /// <summary>
        ///     If true, this command will only work after `devcommands` is run in the console.
        /// </summary>
        public virtual bool IsCheat => false;

        /// <summary>
        ///     If true, this command will only be allowed in networked play.
        /// </summary>
        public virtual bool IsNetwork => false;

        /// <summary>
        ///     If true, and IsNetwork is true, this command will only be allowed in networked play, but only for the server.
        /// </summary>
        public virtual bool OnlyServer => false;

        /// <summary>
        ///     If true, this command will not be shown when the user types `help` into their console.
        /// </summary>
        public virtual bool IsSecret => false;

        /// <summary>
        ///     The function that will be called when the user runs your console command, with space-delimited arguments.
        /// </summary>
        /// <param name="args">The arguments the user types, with spaces being the delimiter.</param>
        public virtual void Run(string[] args)
        {
            
        }

        /// <summary>
        ///     The function that will be called when the user runs your console command, with space-delimited arguments and
        ///     a context <see cref="Terminal"/> object (Console or Chat). Use context?.AddString() to write to the respective
        ///     output.
        /// </summary>
        /// <param name="args">The arguments the user types, with spaces being the delimiter.</param>
        /// <param name="context">Terminal from which this command is run (Console or Chat window)</param>
        public virtual void Run(string[] args, Terminal context)
        {
            // CommandManager always wires this method to the vanilla Terminal commands
            // so we call the one without context one for backward compatibility
            Run(args);
        }
        
        /// <summary>
        ///     Override this function to return a list of strings that are valid options for your command
        /// </summary>
        /// <returns>List of valid command options</returns>
        public virtual List<string> CommandOptionList()
        {
            return null;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Name.ToLower();
        }
    }
}
