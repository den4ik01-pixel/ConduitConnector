using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Electrical;
using System.Windows;

namespace ConduitConnector
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class ConnectorCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Document doc = commandData.Application.ActiveUIDocument.Document;
                ICollection<ElementId> elementIds = commandData.Application.ActiveUIDocument.Selection.GetElementIds();
                List<Element> elems = new List<Element>();

                foreach (ElementId id in elementIds)
                    elems.Add(doc.GetElement(id));

                
                if (!elems.All(elem => elem is Conduit))
                    throw new Exception("You have to select only conduits you want to connect with each other");
                if (elems.Count % 2 != 0)
                    throw new Exception("You have to select an even amount of conduits");
                if (!ConnectionHelper.checkConduitsParallelism(elems))
                    throw new Exception("Selected conduits are not paralel");

                SettingsControl settingsControl = new SettingsControl();
                Window form = new Window() { Width = 830, Height = 245 };
                form.Content = settingsControl;
                form.ShowDialog();

                if (settingsControl.WasConnectBtnClicked)
                {
                    if (elems.Count == 2)
                    {
                        Connector con1, con2;
                        double katetA, katetBNeeded, katetBCurrent, hypotenuse;
                        LocationCurve loc1 = (elems[0].Location as LocationCurve);
                        LocationCurve loc2 = (elems[1].Location as LocationCurve);

                        ConnectionHelper.getNeededConnectors(elems[0], elems[1], out con1, out con2);
                        ConnectionHelper.calcTriangular(con1, con2, settingsControl.Angle, loc1, loc2, out katetA, out katetBNeeded, out katetBCurrent, out hypotenuse);

                        XYZ B = loc1.Curve.GetEndPoint(0);
                        XYZ C = loc1.Curve.GetEndPoint(1);
                        if (C.X == con1.Origin.X && C.Y == con1.Origin.Y && C.Z == con1.Origin.Z)
                        {
                            B = loc1.Curve.GetEndPoint(1);
                            C = loc1.Curve.GetEndPoint(0);
                        }

                        XYZ A = B + 1 * ((katetBCurrent - katetBNeeded)) * (B - C).Normalize();

                        using (Transaction trans = new Transaction(doc))
                        {
                            trans.Start("connector");

                            loc1.Curve = Line.CreateBound(A, C);

                            ConnectionHelper.getNeededConnectors(elems[0], elems[1], out con1, out con2);
                            Conduit conduit = Conduit.Create(doc, elems[0].GetTypeId(), con1.Origin, con2.Origin, elems[0].LevelId);
                            Parameter diameter = conduit.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM);
                            diameter.Set((elems[0] as Conduit).Diameter);

                            ConnectionHelper.getNeededConnectors(elems[0], conduit, out con1, out con2);
                            FamilyInstance elbow1 = doc.Create.NewElbowFitting(con1, con2);

                            ConnectionHelper.getNeededConnectors(elems[1], conduit, out con1, out con2);
                            FamilyInstance elbow2 = doc.Create.NewElbowFitting(con1, con2);

                            trans.Commit();
                        }
                    }
                    else
                    {
                        Connector con1, con2;
                        LocationCurve loc1, loc2;
                        XYZ A, B, C;
                        double katetA, katetBNeeded, katetBCurrent, hypotenuse;
                        List<Conduit> column1Conduits = new List<Conduit>(), column2Conduits = new List<Conduit>();
                        ConnectionHelper.SeparateConduits(elems, column1Conduits, column2Conduits);


                        using (Transaction trans = new Transaction(doc))
                        {
                            trans.Start("connector");

                            for (int i = 0; i < column1Conduits.Count; i++)
                            {
                                loc1 = (column1Conduits[i].Location as LocationCurve);
                                loc2 = (column2Conduits[i].Location as LocationCurve);

                                ConnectionHelper.getNeededConnectors(column1Conduits[i], column2Conduits[i], out con1, out con2);
                                ConnectionHelper.calcTriangular(con1, con2, settingsControl.Angle, loc1, loc2, out katetA, out katetBNeeded, out katetBCurrent, out hypotenuse);

                                B = loc1.Curve.GetEndPoint(0);
                                C = loc1.Curve.GetEndPoint(1);
                                if (C.X == con1.Origin.X && C.Y == con1.Origin.Y && C.Z == con1.Origin.Z)
                                {
                                    B = loc1.Curve.GetEndPoint(1);
                                    C = loc1.Curve.GetEndPoint(0);
                                }

                                A = B + 1 * ((katetBCurrent - katetBNeeded)) * (B - C).Normalize();

                                loc1.Curve = Line.CreateBound(A, C);

                                ConnectionHelper.getNeededConnectors(column1Conduits[i], column2Conduits[i], out con1, out con2);
                                Conduit conduit = Conduit.Create(doc, column1Conduits[i].GetTypeId(), con1.Origin, con2.Origin, column1Conduits[i].LevelId);
                                Parameter diameter = conduit.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM);
                                diameter.Set(column1Conduits[i].Diameter);

                                ConnectionHelper.getNeededConnectors(column1Conduits[i], conduit, out con1, out con2);
                                FamilyInstance elbow1 = doc.Create.NewElbowFitting(con1, con2);

                                ConnectionHelper.getNeededConnectors(column2Conduits[i], conduit, out con1, out con2);
                                FamilyInstance elbow2 = doc.Create.NewElbowFitting(con1, con2);
                            }

                            trans.Commit();
                        }
                    }
                }
            }
            catch(Exception ex) 
            {
                TaskDialog.Show("Error", ex.Message);
            }

            return Result.Succeeded;
        }
    }
}
