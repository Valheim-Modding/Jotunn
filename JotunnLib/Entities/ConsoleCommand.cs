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
        ///     The function that will be called when the user runs your console command, with space-delimited arguments.
        /// </summary>
        /// <param name="args">The arguments the user types, with spaces being the delimiter.</param>
        public abstract void Run(string[] args);

        /// <inheritdoc/>
        public override string ToString()
        {
            return Name;
        }
    }
}
