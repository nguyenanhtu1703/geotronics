using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using App.AnhTu;
using Npgsql.LegacyPostgis;
using System.Collections.Generic;

namespace AppUnitTest
{
    [TestClass]
    public class RandomSolverTest
    {
        [TestMethod]
        public void DistanceTwoGeoPoint_CompareDistance_EqualExpected()
        {
            RandomPointSolver randomPointSolver = new RandomPointSolver();

            Assert.AreEqual(5034, randomPointSolver.DistanceTwoGeoPoint(new PostgisPoint(4.13388889, 50.36638889), new PostgisPoint(71.04083333, 42.35111111)), 1);
            Assert.AreEqual(3, Math.Round(randomPointSolver.DistanceTwoGeoPoint(new PostgisPoint(-121.887340, 49.243824), new PostgisPoint(-121.92532, 49.235347))));
            Assert.AreEqual(3936, Math.Round(randomPointSolver.DistanceTwoGeoPoint(new PostgisPoint(74.00600, 40.7128), new PostgisPoint(118.2437, 34.0522))));
            Assert.AreEqual(2, Math.Round(randomPointSolver.DistanceTwoGeoPoint(new PostgisPoint(-1.7297222222222221, 53.32055555555556), new PostgisPoint(-1.6997222222222223, 53.31861111111111))));
        }

        [TestMethod]
        public void IsPointsApart30km_CompareFarPoints_TrueExpected()
        {
            List<PostgisPoint> points = new List<PostgisPoint>();
            points.Add(new PostgisPoint(4.13388889, 50.36638889));
            points.Add(new PostgisPoint(71.04083333, 42.35111111));

            RandomPointSolver randomPointSolver = new RandomPointSolver();
            Assert.IsTrue(randomPointSolver.IsPointsApart30km(points));
        }

        [TestMethod]
        public void IsPointsApart30km_CompareNearPoints_FalseExpected()
        {
            List<PostgisPoint> points = new List<PostgisPoint>();
            points.Add(new PostgisPoint(-1.7297222222222221, 53.32055555555556));
            points.Add(new PostgisPoint(-1.6997222222222223, 53.31861111111111));

            RandomPointSolver randomPointSolver = new RandomPointSolver();
            Assert.IsFalse(randomPointSolver.IsPointsApart30km(points));
        }

       
        [TestMethod]
        public void DoesRayIntersectEdge_TestAPointLyingOnTheEdge_InsideExpected()
        {
            RandomPointSolver randomPointSolver = new RandomPointSolver();

            PostgisPoint point = new PostgisPoint(3, 2);
            PostgisPoint pointA = new PostgisPoint(3, 0);
            PostgisPoint pointB = new PostgisPoint(3, 3);

            Assert.AreEqual(0, randomPointSolver.DoesRayIntersectEdge(point, pointA, pointB));
        }

        [TestMethod]
        public void LineEquation_TestValue_EqualExpected()
        {
            RandomPointSolver randomPointSolver = new RandomPointSolver();

            PostgisPoint point = new PostgisPoint(2, 0);
            PostgisPoint pointA = new PostgisPoint(0, 0);
            PostgisPoint pointB = new PostgisPoint(6, 6);

            Assert.AreEqual(12, randomPointSolver.LineEquation(point, pointA, pointB), randomPointSolver.Esp);
        }

        [TestMethod]
        public void LineEquation_TestValue2_EqualExpected()
        {
            RandomPointSolver randomPointSolver = new RandomPointSolver();

            PostgisPoint point = new PostgisPoint(2, 2);
            PostgisPoint pointA = new PostgisPoint(1, 1);
            PostgisPoint pointB = new PostgisPoint(6, 6);

            Assert.AreEqual(0, randomPointSolver.LineEquation(point, pointA, pointB), randomPointSolver.Esp);
        }

        [TestMethod]
        public void DoesRayIntersectEdge_TestAPointNotLyingOnTheEdge_InsideExpected()
        {
            RandomPointSolver randomPointSolver = new RandomPointSolver();

            PostgisPoint point = new PostgisPoint(3, -0.1);
            PostgisPoint pointA = new PostgisPoint(3, 0);
            PostgisPoint pointB = new PostgisPoint(3, 3);

            // this is special case when the ray parallrel with with the edge, return -1 
            Assert.AreEqual(-1, randomPointSolver.DoesRayIntersectEdge(point, pointA, pointB));
        }

        [TestMethod]
        public void DoesRayIntersectEdge_TestTheRayNotLyingOnTheEdgeAndNOTCutTheEdge_InsideExpected()
        {
            RandomPointSolver randomPointSolver = new RandomPointSolver();

            PostgisPoint point = new PostgisPoint(2, 0);
            PostgisPoint pointA = new PostgisPoint(0, 0);
            PostgisPoint pointB = new PostgisPoint(6, 6);

            Assert.AreEqual(-1, randomPointSolver.DoesRayIntersectEdge(point, pointA, pointB));
        }

        [TestMethod]
        public void IsPointInsidePolygon_TestAPointInAPolygon_InsideExpected()
        {
            RandomPointSolver randomPointSolver = new RandomPointSolver();

            List<PostgisPoint> points = new List<PostgisPoint>();
            points.Add(new PostgisPoint(0, 0));
            points.Add(new PostgisPoint(4, 0));
            points.Add(new PostgisPoint(4, 4));
            points.Add(new PostgisPoint(0, 4));

            Assert.IsTrue(randomPointSolver.IsPointInsidePolygon(new PostgisPoint(2, 2), points));
        }

        [TestMethod]
        public void IsPointInsidePolygon_TestAPointInAnEdgeOfPolygon_InsideExpected()
        {
            RandomPointSolver randomPointSolver = new RandomPointSolver();

            List<PostgisPoint> points = new List<PostgisPoint>();
            points.Add(new PostgisPoint(0, 0));
            points.Add(new PostgisPoint(4, 0));
            points.Add(new PostgisPoint(4, 4));
            points.Add(new PostgisPoint(0, 4));

            Assert.IsTrue(randomPointSolver.IsPointInsidePolygon(new PostgisPoint(4, 2), points));
        }

        [TestMethod]
        public void IsPointInsidePolygon_TestAPointInOutsideOfPolygon_InsideExpected()
        {
            RandomPointSolver randomPointSolver = new RandomPointSolver();

            List<PostgisPoint> points = new List<PostgisPoint>();
            points.Add(new PostgisPoint(0, 0));
            points.Add(new PostgisPoint(4, 0));
            points.Add(new PostgisPoint(4, 4));
            points.Add(new PostgisPoint(0, 4));

            Assert.IsFalse(randomPointSolver.IsPointInsidePolygon(new PostgisPoint(4.1, 2), points));
        }
    }
}
