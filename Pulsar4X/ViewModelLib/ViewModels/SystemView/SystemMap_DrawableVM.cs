using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;
using System.Windows;
using System.Collections.ObjectModel;

namespace Pulsar4X.ViewModel.SystemView
{
    public class SystemMap_DrawableVM : ViewModelBase
    {
        public List<SystemObjectGraphicsInfo> SystemBodies { get; } = new List<SystemObjectGraphicsInfo>();

        //public List<SystemObjectGraphicsInfo> SystemBodies { get { return _systemBodies; }
        //    set { _systemBodies = value; OnPropertyChanged(); } }

            //private List<SystemObjectGraphicsInfo> _systemBodies = new List<SystemObjectGraphicsInfo>();

        public VectorGraphicDataBase BackGroundHud { get; set; } = new VectorGraphicDataBase();
    
        public List<float> scale_data;

        public void Initialise(GameVM gameVM, StarSystem starSys, AuthenticationToken authToken, List<float> scale_data)
        {
            SystemBodies.Clear();
            this.scale_data = scale_data;

            foreach (var item in starSys.SystemManager.GetAllEntitiesWithDataBlob<StarInfoDB>(authToken))
            {
                SystemBodies.Add(new SystemObjectGraphicsInfo(item, gameVM));
            }
            foreach (var item in starSys.SystemManager.GetAllEntitiesWithDataBlob<SystemBodyDB>(authToken))
            {
                SystemBodies.Add(new SystemObjectGraphicsInfo(item, gameVM));
            }

            PenData hudPen = new PenData();
            hudPen.Alpha = 100;
            hudPen.Red = 255;
            hudPen.Green = 255;
            hudPen.Blue = 255;

            List<VectorShapeBase> hudShapes1 = new List<VectorShapeBase>();
            hudShapes1.Add(new LineData(0, -200, 0, -150));


            hudShapes1.Add(new LineData(0, -150, 2, -150));
            hudShapes1.Add(new LineData(0, -150, 0, -100));
            hudShapes1.Add(new LineData(0, -100, 2, -100));
            hudShapes1.Add(new LineData(0, -100, 0, -50));
            hudShapes1.Add(new LineData(0, -50, 2, -50));
            hudShapes1.Add(new LineData(0, -50, 0, 50));
            hudShapes1.Add(new LineData(0, 50, 2, 50));
            hudShapes1.Add(new LineData(0, 50, 0, 100));
            hudShapes1.Add(new LineData(0, 100, 2, 100));
            hudShapes1.Add(new LineData(0, 100, 0, 150));
            hudShapes1.Add(new LineData(0, 150, 2, 150));
            hudShapes1.Add(new LineData(0, 150, 0, 200));


            List<VectorShapeBase> hudShapes2 = new List<VectorShapeBase>();
            //hudShapes2.Add(new LineData(200, 0, -200, 0));
            hudShapes2.Add(new LineData(-200, 0, -150, 0));
            hudShapes2.Add(new LineData(-150, 2, -150, 0));
            hudShapes2.Add(new LineData(-150, 0, -100, 0));
            hudShapes2.Add(new LineData(-100, 2, -100, 0));
            hudShapes2.Add(new LineData(-100, 0, -50, 0));
            hudShapes2.Add(new LineData(-50, 2, -50, 0));
            hudShapes2.Add(new LineData(-50, 0, 50, 0));
            hudShapes2.Add(new LineData(50, 2, 50, 0));
            hudShapes2.Add(new LineData(50, 0, 100, 0));
            hudShapes2.Add(new LineData(100, 2, 100, 0));
            hudShapes2.Add(new LineData(100, 0, 150, 0));
            hudShapes2.Add(new LineData(150, 2, 150, 0));
            hudShapes2.Add(new LineData(150, 0, 200, 0));


            BackGroundHud.PathList.Add(new VectorPathPenPair(hudPen, hudShapes1));
            BackGroundHud.PathList.Add(new VectorPathPenPair(hudPen, hudShapes2));
            BackGroundHud.SizeAffectedbyZoom = true;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SystemBodies));
        }        
    }

    public class SystemObjectGraphicsInfo
    {
        private Entity item;
        public IconData Icon { get; set; }
        public TextData NameString { get; set; }
        public OrbitEllipseFading OrbitEllipse { get; set; }
        public OrbitEllipseSimple SimpleOrbitEllipse { get; set; }
        public OrbitEllipseSimpleFading SimpleOrbitEllipseFading { get; set; }
        public DateTime CurrentDate { get; }
        public SystemObjectGraphicsInfo(Entity item, GameVM gameviewModel)
        {
            Icon = new IconData(item);
            NameString = new TextData(item.GetDataBlob<NameDB>().GetName(gameviewModel.CurrentFaction), (float)item.GetDataBlob<PositionDB>().X, (float)item.GetDataBlob<PositionDB>().Y, 8);
            Icon.PathList.Add(new VectorPathPenPair(NameString));

            if (item.HasDataBlob<OrbitDB>() && !item.GetDataBlob<OrbitDB>().IsStationary)
            {
                switch (item.GetDataBlob<SystemBodyDB>()?.Type)
                {
                    case BodyType.Comet:
                        SimpleOrbitEllipseFading = new OrbitEllipseSimpleFading(item);
                        break;
                    case BodyType.DwarfPlanet:
                        SimpleOrbitEllipse = new OrbitEllipseSimple(item.GetDataBlob<OrbitDB>());
                        break;
                    case BodyType.Moon:
                        SimpleOrbitEllipseFading = new OrbitEllipseSimpleFading(item);
                        break;
                    case BodyType.GasDwarf:
                        SimpleOrbitEllipseFading = new OrbitEllipseSimpleFading(item);
                        break;
                    case BodyType.GasGiant:
                        SimpleOrbitEllipseFading = new OrbitEllipseSimpleFading(item);
                        break;
                    case BodyType.IceGiant:
                        SimpleOrbitEllipseFading = new OrbitEllipseSimpleFading(item);
                        break;
                    case BodyType.Terrestrial:
                        SimpleOrbitEllipseFading = new OrbitEllipseSimpleFading(item);
                        break;
                    case BodyType.Asteroid:
                        break;
                    default:
                        break;
                }

                //OrbitEllipse = new OrbitEllipseFading(item.GetDataBlob<OrbitDB>(), item.GetDataBlob<OrbitDB>().Parent.GetDataBlob<PositionDB>());
                //SimpleOrbitEllipse = new OrbitEllipseSimple(item.GetDataBlob<OrbitDB>());
                //SimpleOrbitEllipseFading = new OrbitEllipseSimpleFading(item);
            }
            gameviewModel.DateChangedEvent += GameviewModel_DateChangedEvent;
        }
        
        private void GameviewModel_DateChangedEvent(DateTime oldDate, DateTime newDate)
        {
            Icon.CurrentDateTime = newDate;
            if(OrbitEllipse != null)
                OrbitEllipse.CurrentDateTime = newDate;
            if (SimpleOrbitEllipseFading != null)
                SimpleOrbitEllipseFading.CurrentDateTime = newDate;
        }
    }


}
