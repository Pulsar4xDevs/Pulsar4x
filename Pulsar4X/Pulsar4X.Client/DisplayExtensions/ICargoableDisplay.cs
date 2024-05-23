using System.Linq;
using Pulsar4X.Components;
using Pulsar4X.Engine.Designs;
using Pulsar4X.Engine.Industry;
using Pulsar4X.Interfaces;

namespace Pulsar4X.SDL2UI;

public static class ICargoableDisplay
{
    public static void ShowTooltip(this ICargoable cargoable)
    {
        if(cargoable is Mineral)
        {
            var mineralSD = (Mineral)cargoable;
            DisplayHelpers.DescriptiveTooltip(cargoable.Name, "Mineral", mineralSD.Description);
        }
        else if(cargoable is ProcessedMaterial)
        {
            var processedMaterialSD = (ProcessedMaterial)cargoable;
            DisplayHelpers.DescriptiveTooltip(cargoable.Name, "Processed Material", processedMaterialSD.Description);
        }
        else if(cargoable is ComponentInstance)
        {
            var componentInstance = (ComponentInstance)cargoable;
            DisplayHelpers.DescriptiveTooltip(cargoable.Name, componentInstance.Design.ComponentType, componentInstance.Design.Description);
        }
        else if(cargoable is ComponentDesign)
        {
            var componentDesign = (ComponentDesign)cargoable;
            DisplayHelpers.DescriptiveTooltip(componentDesign.Name, componentDesign.ComponentType, componentDesign.Description);
        }
        else if(cargoable is OrdnanceDesign)
        {
            var ordnanceDesign = (OrdnanceDesign)cargoable;
            var components = ordnanceDesign.Components.Select(tuple => tuple.design).ToArray();
            foreach(var component in components)
            {
                DisplayHelpers.DescriptiveTooltip(component.Name, component.ComponentType, component.Description);
            }
        }
    }
}