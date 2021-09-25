using System;
using TrueCraft.API.Server;
using TrueCraft.API.Windows;
using TrueCraft.API.World;

namespace TrueCraft.Core.Windows
{
    public interface IFurnaceWindowContent : IWindowContent
    {
        ISlots Ingredient { get; }

        /// <summary>
        /// Gets the Slot Index within the overall Window Content at which
        /// the smelting Ingredient is located.
        /// </summary>
        int IngredientIndex { get; }

        ISlots Fuel { get; }

        /// <summary>
        /// Gets the Slot Index within the overall Window Content at which
        /// the Fuel is located.
        /// </summary>
        int FuelIndex { get; }

        ISlots Output { get; }

        /// <summary>
        /// Gets the Slot Index within the overall Window Content at which
        /// the Output is located.
        /// </summary>
        int OutputIndex { get; }
    }
}
