using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConduitConnector
{
    public class ConnectionHelper
    {
        #region Public Methods

        //Checks if all input conduits are parallel. Returns true if all conduits are parallel, otherwise false
        public static bool checkConduitsParallelism(List<Element> elems)
        {
            Curve tempCurve1, tempCurve2;

            for (int i = 1; i < elems.Count; i++)
            {
                tempCurve1 = (elems[i - 1].Location as LocationCurve).Curve;
                tempCurve2 = (elems[i].Location as LocationCurve).Curve;

                tempCurve1.MakeUnbound();
                tempCurve2.MakeUnbound();

                if (tempCurve1.Intersect(tempCurve2) != SetComparisonResult.Disjoint)
                    return false;

            }

            return true;
        }

        //Find left and right conduits and place them in needed list
        public static void SeparateConduits(List<Element> elems, List<Conduit> column1Conduits, List<Conduit> column2Conduits)
        {
            int firstInd = -1, secondInd = -1;
            double dist = -1, tempDist1, tempDist2;
            Curve tempCurve;

            List<Conduit> conduits = new List<Conduit>();
            foreach (Element elem in elems)
                conduits.Add(elem as Conduit);

            //finding the farthest conduit
            for (int i = 0; i < conduits.Count; i++)
            {
                tempCurve = (conduits[i].Location as LocationCurve).Curve;
                tempCurve.MakeUnbound();
                for (int q = (i + 1); q < conduits.Count; q++)
                {
                    tempDist1 = tempCurve.Distance((conduits[q].Location as LocationCurve).Curve.GetEndPoint(0));
                    tempDist2 = tempCurve.Distance((conduits[q].Location as LocationCurve).Curve.GetEndPoint(1));

                    if (dist < (tempDist2 > tempDist1 ? tempDist1 : tempDist2))
                    {
                        firstInd = i;
                        secondInd = q;
                        dist = (tempDist2 > tempDist1 ? tempDist1 : tempDist2);
                    }
                }
            }

            column1Conduits.Add(conduits[firstInd]);
            column2Conduits.Add(conduits[secondInd]);
            conduits.RemoveAll(con => con == column1Conduits[0] || con == column2Conduits[0]);

            calcClosestConduits(column1Conduits, conduits);
            calcClosestConduits(column2Conduits, conduits);

            column2Conduits.Reverse();
        }

        //Finds the closest connectors of two input elements and initializes two 'out' input params(con1, con2) with these closest connectors
        public static void getNeededConnectors(Element elem1, Element elem2, out Connector con1,
            out Connector con2)
        {
            con1 = con2 = null;
            List<Connector> elem1Connectors, elem2Connectors;
            getConnectors(elem1, out elem1Connectors);
            getConnectors(elem2, out elem2Connectors);

            double result = double.MaxValue, tempResult;
            for (int i = 0; i < elem1Connectors.Count; i++)
                for (int q = 0; q < elem2Connectors.Count; q++)
                {
                    tempResult = elem1Connectors[i].Origin.DistanceTo(elem2Connectors[q].Origin);

                    if (tempResult < result)
                    {
                        result = tempResult;
                        con1 = elem1Connectors[i];
                        con2 = elem2Connectors[q];
                    }
                }
        }

        //Calculate all parts of the triangular which is formed by 'con1' and 'con2'
        public static void calcTriangular
            (Connector con1, Connector con2, double angleNeeded, LocationCurve loc1, LocationCurve loc2, out double katetA,
            out double katetBNeeded, out double katetBCurrent, out double hypotenuseNeeded)
        {
            double hypotenuse = con1.Origin.DistanceTo(con2.Origin);
            double radiansNeed = angleNeeded * Math.PI / 180;
            Curve curve1 = loc1.Curve;
            Curve curve2 = loc2.Curve;

            curve1.MakeUnbound();
            double dist1 = curve1.Distance(curve2.GetEndPoint(0));
            double dist2 = curve1.Distance(curve2.GetEndPoint(1));


            katetA = dist1 > dist2 ? dist2 : dist1;
            katetBCurrent = Math.Sqrt(hypotenuse * hypotenuse - katetA * katetA);
            katetBNeeded = katetA * (1 / Math.Tan(radiansNeed));
            hypotenuseNeeded = Math.Sqrt((katetA * katetA) + (katetBNeeded * katetBNeeded));
        }

        #endregion

        #region Additional private methods

        //Fill 'out List<Connector> connectors' with all 'elem' connectors
        private static void getConnectors(Element elem, out List<Connector> connectors)
        {
            connectors = new List<Connector>();
            ConnectorSet set = (elem as Conduit).ConnectorManager.Connectors;

            foreach (Connector con in set)
                connectors.Add(con);
        }

        private static void calcClosestConduits(List<Conduit> columnConduits, List<Conduit> conduits)
        {
            if (conduits.Count == 1)
            {
                columnConduits.Add(conduits[0]);
                return;
            }

            int startCount = columnConduits.Count;
            double dist = double.MaxValue, tempDist1, tempDist2;
            Curve tempCurve = (columnConduits[0].Location as LocationCurve).Curve;
            for (int i = conduits.Count / 2 - 1, closestConduitInd = 0; i >= 0; i--)
            {
                for (int q = 0; q < conduits.Count; q++)
                {
                    tempDist1 = tempCurve.Distance((conduits[q].Location as LocationCurve).Curve.GetEndPoint(0));
                    tempDist2 = tempCurve.Distance((conduits[q].Location as LocationCurve).Curve.GetEndPoint(1));

                    if (dist > (tempDist2 > tempDist1 ? tempDist1 : tempDist2))
                    {
                        closestConduitInd = q;
                        dist = (tempDist2 > tempDist1 ? tempDist1 : tempDist2);
                    }
                }

                dist = double.MaxValue;
                columnConduits.Add(conduits[closestConduitInd]);
                tempCurve = (columnConduits[columnConduits.Count - 1].Location as LocationCurve).Curve;
                tempCurve.MakeUnbound();
                conduits.RemoveAt(closestConduitInd);

                if (conduits.Count == 1 && i == 0)
                {
                    columnConduits.Add(conduits[0]);
                    conduits.RemoveAt(0);
                }
            }

            /*if (conduits.Count == 1 && columnConduits.Count == startCount)
            {
                columnConduits.Add(conduits[0]);
                conduits.RemoveAt(0);
            }*/
        }

        #endregion
    }
}
