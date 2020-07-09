using System.Collections.Generic;
using VaultLib.Core;

namespace Attribulator.API.Data
{
    /// <summary>
    /// Represents a file loaded into a database.
    /// </summary>
    public class LoadedFile
    {
        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Gets the group ID of the file.
        /// </summary>
        public string Group { get; }
        
        /// <summary>
        /// Gets the vaults loaded from the file.
        /// </summary>
        public IEnumerable<Vault> Vaults { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadedFile"/> class.
        /// </summary>
        /// <param name="name">The name of the file.</param>
        /// <param name="group"></param>
        /// <param name="vaults">The list of vaults in the file.</param>
        public LoadedFile(string name, string group, IEnumerable<Vault> vaults)
        {
            this.Name = name;
            this.Group = group;
            this.Vaults = vaults;
        }
    }
}