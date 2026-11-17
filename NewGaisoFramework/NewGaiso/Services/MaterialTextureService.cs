using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Ymd.Gaiso.Simulator.Services
{
    public static class MaterialTextureService
    {
        public static Material CreateHelix3DMaterial(Models.PanelComponent comp)
        {
            Color c = comp != null && comp.Color != null && comp.Color.Contains("ブラック") ? Colors.DarkSlateGray : Colors.LightGray;
            return new DiffuseMaterial(new SolidColorBrush(c));
        }
        
        public static Material CreateFallbackMaterial()
        {
            return new DiffuseMaterial(new SolidColorBrush(Colors.LightSteelBlue));
        }
    }
}
