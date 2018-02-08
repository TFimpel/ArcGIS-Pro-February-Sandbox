using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace ProAppModule2
{


    /// <summary>
    /// Interaction logic for Dockpane1View.xaml
    /// </summary>
    public partial class Dockpane1View : UserControl
    {
        public Dockpane1View()
        {
            InitializeComponent();


        }




        public async void btnAddLayer1_ClickAsync(object sender, EventArgs e)
        {
            string button1_lyr_url = "https://services.arcgis.com/BG6nSlhZSAWtExvp/arcgis/rest/services/US_Rivers/FeatureServer/0";

            await AddLayer(button1_lyr_url);
        }


        public async Task<Layer> AddLayer(string uri)
        {
            return await QueuedTask.Run(() =>
             {
                 Map map = MapView.Active.Map;
                 return LayerFactory.Instance.CreateLayer(new Uri(uri), map);
             });
        }


        public async void btnChangeLayerSymbology(object sender, EventArgs e)
        {
            //Debug.WriteLine(sender);
            var lineStyle = ((ListBoxItem)sender).Tag.ToString();
            //Debug.WriteLine(t);
            await GetFeatureLayerFromMapAndChangeSymbology(lineStyle);
        }


        public async void btnChangeLayerLabels(object sender, EventArgs e)
        {
            await GetFeatureLayerFromMapAndChangeLabels();
        }


        public async void btnChangeLayerFilter(object sender, EventArgs e)
        {
            var featureName = ((RadioButton)sender).Tag;
            await GetFeatureLayerFromMapAndChangeFilter(featureName.ToString());
        }


        public async void btnAddMapToNewLayout(object sender, EventArgs e)
        {
           await makeNewLayout();
        }

        private readonly object _lockLegend = new object();

        public async void btnModifyLegend(object sender, EventArgs e)
        {
            await modifyLegend();
        }

        public async Task modifyLegend()
        {
            await QueuedTask.Run(() =>
            {
                LayoutProjectItem layoutItem = Project.Current.GetItems<LayoutProjectItem>().FirstOrDefault(item => item.Name.Equals("MY NEW LAYOUT"));

                Layout layout = layoutItem.GetLayout();
                if (layout != null)
                {
                    Element element = layout.FindElement("New Legend");
                    if (element != null)
                    {
                        CIMLegend CIMLegend = element.GetDefinition() as CIMLegend;
                        CIMLegend.Locked = true;

                        foreach (var legendItem in CIMLegend.Items)
                        {

                            var itemLabel = legendItem.LabelSymbol.Symbol as CIMTextSymbol;
                            foreach (var symlayer in ((CIMPolygonSymbol)itemLabel.Symbol).SymbolLayers)
                            {
                                if (symlayer is ArcGIS.Core.CIM.CIMSolidFill)
                                {
                                    var itemLabelSym = (CIMSolidFill)symlayer;
                                    itemLabelSym.Color.Values[0] = 0;
                                    itemLabelSym.Color.Values[1] = 0;
                                    itemLabelSym.Color.Values[2] = 255;

                                }
                            }
                        }
                        element.SetDefinition(CIMLegend);
                    }
                }
            });
        }

        public async void btnExportLayout(object sender, EventArgs e)
        {
            LayoutProjectItem layoutItem = Project.Current.GetItems<LayoutProjectItem>().FirstOrDefault(item => item.Name.Equals("MY NEW LAYOUT"));
            if (layoutItem != null)
            {
                await QueuedTask.Run(() =>
                {
                    Layout layout = layoutItem.GetLayout();
                    if (layout == null)
                        return;

                    PDFFormat PDF = new PDFFormat()
                    {
                        Resolution = 300,
                        OutputFileName = @"C:\Users\fimpe\OneDrive\MGIS\GEOG 8990 Spring 2018\Layout.pdf"
                    };
                    if (PDF.ValidateOutputFilePath())
                    {
                        layout.Export(PDF);
                        MessageBox.Show("The layout was exported to a pdf.", "Well done!", MessageBoxButton.OK);
                    }
                });
            }
        }

        public static async Task addLegendToLayout(Layout layout)
        {
            await QueuedTask.Run(() =>
            {
                //Build 2D envelope geometry
                Coordinate2D leg_ll = new Coordinate2D(1, 1);
                Coordinate2D leg_ur = new Coordinate2D(7.5, 3);
                Envelope leg_env = EnvelopeBuilder.CreateEnvelope(leg_ll, leg_ur);

                //Reference MF, create legend and add to layout
                MapFrame mf = layout.FindElement("My New Map Frame") as MapFrame;
                Legend legendElm = LayoutElementFactory.Instance.CreateLegend(layout, leg_env, mf);
                legendElm.SetName("New Legend");
            });
        }


        public static async Task makeNewLayout()
        {
                Layout newLayout = await QueuedTask.Run<Layout>(() =>
                {
                    newLayout = LayoutFactory.Instance.CreateLayout(8.5, 11, LinearUnit.Inches);
                    newLayout.SetName("MY NEW LAYOUT");
                    return newLayout;
                });
           await AddMapToNewLayout(newLayout);
           await addLegendToLayout(newLayout);
           await AddTtitleToNewLayout(newLayout);


            ILayoutPane iNewLayoutPane = await ProApp.Panes.CreateLayoutPaneAsync(newLayout);
        }

        public static async Task AddTtitleToNewLayout(Layout layout)
        {
            await QueuedTask.Run(() =>
            {
                //var title = @"<dyn type = ""page"" property = ""name"" />";
                var title = "this is the title";
                Coordinate2D llTitle = new Coordinate2D(1, 9.5);
                //Note: call within QueuedTask.Run()
                var titleGraphics = LayoutElementFactory.Instance.CreatePointTextGraphicElement(layout, llTitle, null) as ArcGIS.Desktop.Layouts.TextElement;
                titleGraphics.SetName("MapTitle");
                titleGraphics.SetTextProperties(new TextProperties(title, "Arial", 24, "Bold"));
            });
        }


        public static async Task AddMapToNewLayout(Layout layout)
        {
            await QueuedTask.Run(() =>
            {
                //Build 2D envelope geometry
                Coordinate2D mf_ll = new Coordinate2D(1, 3.5);
                Coordinate2D mf_ur = new Coordinate2D(7.5, 9.5);
                Envelope mf_env = EnvelopeBuilder.CreateEnvelope(mf_ll, mf_ur);

                //Reference map, create MF and add to layout
                MapProjectItem mapPrjItem = Project.Current.GetItems<MapProjectItem>().FirstOrDefault(item => item.Name.Equals("Map"));
                Map mfMap = mapPrjItem.GetMap();

                MapFrame mfElm = LayoutElementFactory.Instance.CreateMapFrame(layout, mf_env, mfMap);
                mfElm.SetName("My New Map Frame");
                

            });
        }


        public async Task GetFeatureLayerFromMapAndChangeLabels()
        {
            var riverslayer = (MapView.Active.Map.Layers.First(layer => true) as FeatureLayer);
            await SimpleLabels(riverslayer);
        }


        public async Task GetFeatureLayerFromMapAndChangeSymbology(String lineStyle)
        {
            //var riverslayer = (MapView.Active.Map.Layers.First(layer => layer.Name.Equals("US_Rivers")) as FeatureLayer);
            var riverslayer = (MapView.Active.Map.Layers.First(layer => true) as FeatureLayer);
            await SimpleRendererLine(riverslayer,lineStyle);
        }


        public async Task GetFeatureLayerFromMapAndChangeFilter(String featureName)
        {
            var riverslayer = (MapView.Active.Map.Layers.First(layer => true) as FeatureLayer);
            await SimpleFilter(riverslayer, featureName);
            await UpdateTitle(featureName);
            await ZoomToLayer(riverslayer);
        }

        internal static Task UpdateTitle(String featureName)
        {
            return QueuedTask.Run(() =>
            {
                LayoutProjectItem layoutItem = Project.Current.GetItems<LayoutProjectItem>().FirstOrDefault(item => item.Name.Equals("MY NEW LAYOUT"));
                Layout layout = layoutItem.GetLayout();
                if (layout != null)
                {
                    ArcGIS.Desktop.Layouts.TextElement txtElm =  layout.FindElement("MapTitle") as ArcGIS.Desktop.Layouts.TextElement;
                    if (txtElm != null)
                    {
                        TextProperties txtProperties = new TextProperties(featureName, "Times New Roman", 24, "Regular");
                        txtElm.SetTextProperties(txtProperties);
                    }
                }
            });
        }

        internal static Task ZoomToLayer(FeatureLayer featurelayer)
        {
            return QueuedTask.Run(() =>
            {
                var mapView = MapView.Active;
                if (mapView == null)
                    return Task.FromResult(false);
                return mapView.ZoomToAsync(featurelayer);
            });
        }


        internal static Task SimpleRendererLine(FeatureLayer featureLayer, String lineStyle)
        {  
            return QueuedTask.Run(() =>
            {
               
                //Create a circle marker
                if (lineStyle == "Dash") {
                    var lineSymbol = SymbolFactory.Instance.ConstructLineSymbol(ColorFactory.Instance.BlueRGB, 3, SimpleLineStyle.Dash);
                    CIMSimpleRenderer renderer = featureLayer.GetRenderer() as CIMSimpleRenderer;
                    renderer.Symbol = lineSymbol.MakeSymbolReference();
                    featureLayer.SetRenderer(renderer);
                }
                else if (lineStyle == "Dot")
                {
                    var lineSymbol = SymbolFactory.Instance.ConstructLineSymbol(ColorFactory.Instance.BlueRGB, 3, SimpleLineStyle.Dot);
                    CIMSimpleRenderer renderer = featureLayer.GetRenderer() as CIMSimpleRenderer;
                    renderer.Symbol = lineSymbol.MakeSymbolReference();
                    featureLayer.SetRenderer(renderer);
                }
                else if (lineStyle == "DashDot")
                {
                    var lineSymbol = SymbolFactory.Instance.ConstructLineSymbol(ColorFactory.Instance.BlueRGB, 3, SimpleLineStyle.DashDot);
                    CIMSimpleRenderer renderer = featureLayer.GetRenderer() as CIMSimpleRenderer;
                    renderer.Symbol = lineSymbol.MakeSymbolReference();
                    featureLayer.SetRenderer(renderer);
                }
                else if (lineStyle == "DashDotDot")
                {
                    var lineSymbol = SymbolFactory.Instance.ConstructLineSymbol(ColorFactory.Instance.BlueRGB, 3, SimpleLineStyle.DashDotDot);
                    CIMSimpleRenderer renderer = featureLayer.GetRenderer() as CIMSimpleRenderer;
                    renderer.Symbol = lineSymbol.MakeSymbolReference();
                    featureLayer.SetRenderer(renderer);
                }
                else if (lineStyle == "Solid")
                {
                    var lineSymbol = SymbolFactory.Instance.ConstructLineSymbol(ColorFactory.Instance.BlueRGB, 3, SimpleLineStyle.Solid);
                    CIMSimpleRenderer renderer = featureLayer.GetRenderer() as CIMSimpleRenderer;
                    renderer.Symbol = lineSymbol.MakeSymbolReference();
                    featureLayer.SetRenderer(renderer);
                }
            });
        }


        internal static Task SimpleLabels(FeatureLayer featureLayer)
        {
            return QueuedTask.Run(() =>
            {
                featureLayer.SetLabelVisibility(true);
            }
            );
        }


        internal static Task SimpleFilter(FeatureLayer featureLayer, String featureName)
        {
            return QueuedTask.Run(() =>
            {
                //featureLayer.SetDefinitionQuery("SYSTEM = 'Mississippi'");
                featureLayer.SetDefinitionQuery("SYSTEM = '" + featureName + "'");
                featureLayer.SetName(featureName);

            }
            );
        }
    }
}
