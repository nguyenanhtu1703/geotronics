using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql.LegacyPostgis;
using EGIS.ShapeFileLib;
using System.Linq;
using System.Text;
using System;
using Npgsql;

namespace AppTest.AnhTu
{
    class Program
    {
        static void Main(string[] args)
        {
            GetPanstwoFileData getPanstwoFileData = new GetPanstwoFileData();
            List<PostgisPoint> points = getPanstwoFileData.GetPointsOfPolygonBorders();

            RandomPointSolver randomPointSolver = new RandomPointSolver();
            List<PostgisPoint> randomedPoints = randomPointSolver.Solve(points);
            Console.WriteLine("are points apart 30km?: {0}\n", randomPointSolver.Test_isPointsApart30km_TrueExpected(randomedPoints));

            DatabaseUltility databaseUltility = new DatabaseUltility();
            databaseUltility.Connect();
            databaseUltility.SaveRandomedPointsIntoPointsTable(randomedPoints);
            databaseUltility.ListRandomedPointsBelongsToOutlines(randomedPoints);
            //databaseUltility.TryAppendRandomedPointToVoivodshipDatabase(randomedPoints);
            //databaseUltility.TryAppendBorderToVoivodshipDatabase(points);

            Console.WriteLine("program finished successful!");
            Console.ReadLine();
        }
    }

    class Configuration
    {
        public static String pathPanstwoShapeFile = "/Panstwo/Państwo.shp";
        public static String pathWojewodztwaShapeFile = "/Wojewodztwa/Województwa.shp";
        public static int randomPointNumber = 200;
        public static float apartDistance = 30;

        public static String hostIP = "127.0.0.1";
        public static int port = 5432;
        public static String username = "demo_user";
        public static String password = "12345";
        public static String database = "firstdb";
        public static String tableName = "points";

        public static String voivodshipTableName = "voivodship";
    }

    class GetPanstwoFileData
    {
        public List<PostgisPoint> GetPointsOfPolygonBorders()
        {
            using (var sf = new ShapeFile(Environment.CurrentDirectory + Configuration.pathPanstwoShapeFile))
            {
                List<PostgisPoint> points = new List<PostgisPoint>();

                for (int i = 0; i < sf.RecordCount; i++)
                {
                    var rings = sf.GetShapeData(i);
                    for (int j = 0; j < rings.Count; j++)
                    {
                        foreach (var point in rings.ElementAt(j))
                        {
                            points.Add(new PostgisPoint(point.X, point.Y));
                        }
                    }
                }

                return points;
            }
        }
    }

    class DatabaseUltility
    {
        public NpgsqlConnection Conn { get; set; }

        public void Connect()
        {
            String connString = String.Format("Host = {0}; Username = {1}; Password =  {2}; Database = {3}", Configuration.hostIP, Configuration.username, Configuration.password, Configuration.database);
            Conn = new NpgsqlConnection(connString);

            Conn.Open();
            Conn.TypeMapper.UseLegacyPostgis();

            Console.WriteLine("connected to postgres!\n");
        }

        public void SaveRandomedPointsIntoPointsTable(List<PostgisPoint> points)
        {
            StringBuilder sqlQuery = new StringBuilder(String.Format("INSERT INTO {0}({1}) VALUES({2}, {3})", Configuration.tableName, "x, y", points.ElementAt(0).X, points.ElementAt(0).Y));

            for (int i = 1; i < points.Count; i++)
                sqlQuery.Append(String.Format(", ({0}, {1})", points.ElementAt(i).X, points.ElementAt(i).Y));

            sqlQuery.Append(";");

            using (var cmd = new NpgsqlCommand(sqlQuery.ToString(), Conn))
            {
                cmd.ExecuteNonQuery();
            }

            Console.WriteLine("saved to postgres randomed points successful!\n");
        }

        public void TryAppendRandomedPointToPanstwoDatabase(List<PostgisPoint> randomedPoints)
        {
            if (randomedPoints.Count == 0)
                return;

            StringBuilder pointGeometryAsText = new StringBuilder(String.Format("POLYGON(({0} {1}, ", randomedPoints.ElementAt(0).X, randomedPoints.ElementAt(0).Y));

            for (int i = 1; i < randomedPoints.Count; i++)
                pointGeometryAsText.Append(String.Format(",{0} {1}", randomedPoints.ElementAt(i).X, randomedPoints.ElementAt(i).Y));

            pointGeometryAsText.Append("))");

            StringBuilder sqlQuery = new StringBuilder(String.Format("INSERT INTO {0}({1}) VALUES('{2}');", "panstwo", "geom", pointGeometryAsText));

            NpgsqlCommand cmd = new NpgsqlCommand(sqlQuery.ToString(), Conn);
            cmd.ExecuteNonQuery(); 
        }

        public void TryAppendRandomedPointToVoivodshipDatabase(List<PostgisPoint> randomedPoints)
        {
            if (randomedPoints.Count == 0)
                return;

            StringBuilder pointGeometryAsText = new StringBuilder(String.Format("GEOMETRYCOLLECTION(POINT({0} {1})", randomedPoints.ElementAt(0).X, randomedPoints.ElementAt(0).Y));

            for (int i = 1; i < randomedPoints.Count; i++)
                pointGeometryAsText.Append(String.Format(",POINT({0} {1})", randomedPoints.ElementAt(i).X, randomedPoints.ElementAt(i).Y));

            pointGeometryAsText.Append(")");

            StringBuilder sqlQuery = new StringBuilder(String.Format("INSERT INTO {0}({1}) VALUES('{2}');", Configuration.voivodshipTableName, "geom", pointGeometryAsText));

            NpgsqlCommand cmd = new NpgsqlCommand(sqlQuery.ToString(), Conn);
            cmd.ExecuteNonQuery();
        }

        public void TryAppendBorderToVoivodshipDatabase(List<PostgisPoint> points)
        {
            if (points.Count == 0)
                return;

            StringBuilder pointGeometryAsText = new StringBuilder(String.Format("GEOMETRYCOLLECTION(POLYGON(({0} {1}", points.ElementAt(0).X, points.ElementAt(0).Y));

            for (int i = 1; i < points.Count; i++)
                pointGeometryAsText.Append(String.Format(", {0} {1}", points.ElementAt(i).X, points.ElementAt(i).Y));

            pointGeometryAsText.Append(")))");

            StringBuilder sqlQuery = new StringBuilder(String.Format("INSERT INTO {0}({1}) VALUES('{2}');", Configuration.voivodshipTableName, "geom", pointGeometryAsText));

            NpgsqlCommand cmd = new NpgsqlCommand(sqlQuery.ToString(), Conn);
            cmd.ExecuteNonQuery();
        }

        public void ListRandomedPointsBelongsToOutlines(List<PostgisPoint> randomedPoints)
        {
            String sqlQuery = String.Format("SELECT {0} FROM {1};", "jpt_nazwa_, geom", Configuration.voivodshipTableName);
            NpgsqlCommand cmd = new NpgsqlCommand(sqlQuery, Conn);

            using (var reader = cmd.ExecuteReader())
            {
                Console.WriteLine("retrived multipolygon from postgres successful!\n");

                PostgisMultiPolygon multiPolygon;
                while (reader.Read())
                {
                    multiPolygon = null;

                    try
                    {
                        String voivodshipName = reader.GetString(0);
                        multiPolygon = (PostgisMultiPolygon)reader.GetValue(1);
                        ListRandomedPointsBelongsToOutlineOfAVoivodship(voivodshipName, randomedPoints, multiPolygon);
                    } catch (Exception e)
                    {
                        //Console.WriteLine(e.StackTrace);
                    }
                }
            }
        }

        public void ListRandomedPointsBelongsToOutlineOfAVoivodship(String s, List<PostgisPoint> points, PostgisMultiPolygon multiPolygon)
        {
            Console.WriteLine("randomed points belong to {0}'s outline are: ", s);
            int count = 0;

            for (int i = 0; i < points.Count; i++)
            {
                if (multiPolygon == null)
                    break;

                bool belong = false;
                foreach (var polygon in multiPolygon)
                {
                    if (IsPointBelongToOutlineOfPolygon(points.ElementAt(i), polygon))
                    {
                        belong = true;
                        break;
                    }
                }
                if (belong)
                {
                    count++;
                    Console.WriteLine(points.ElementAt(i).X + ", " + points.ElementAt(i).Y);
                }
            }

            Console.WriteLine("there is total {0} points belong to its outline\n", count);
        }

        /*
         * check for all the edges if the point belong to any edge
         */ 
        bool IsPointBelongToOutlineOfPolygon(PostgisPoint point, PostgisPolygon polygon)
        {
            RandomPointSolver randomPointSolver = new RandomPointSolver();
            double espBelong = 1e-5;

            foreach (var ring in polygon)
            {
                Coordinate2D[] coordinates = ring.ToArray();

                for (int i = 1; i < coordinates.Length; i++)
                {
                    if (point.X > Math.Max(coordinates[i].X, coordinates[i - 1].X) + espBelong || point.X < Math.Min(coordinates[i].X, coordinates[i - 1].X - espBelong)
                       || point.Y > Math.Max(coordinates[i].Y, coordinates[i - 1].Y) + espBelong || point.Y < Math.Min(coordinates[i].Y, coordinates[i - 1].Y) - espBelong)
                        continue;

                    // point on the edge
                    if (Math.Abs(randomPointSolver.LineEquation(point, coordinates[i - 1], coordinates[i])) <= espBelong)
                        return true;
                }
            }

            return false;
        }
    }

    class RandomPointSolver {
        public double Esp { set; get; }

        public RandomPointSolver()
        {
            Esp = 1e-32;
        }

        public List<PostgisPoint> Solve(List<PostgisPoint> polygonPoints)
        {
            var startTime = System.DateTime.Now;

            List<PostgisPoint> result = new List<PostgisPoint>();

            double maxX = Double.MinValue;
            double minX = Double.MaxValue;
            double maxY = Double.MinValue;
            double minY = Double.MaxValue;

            for(int i = 0; i < polygonPoints.Count; i++)
            {
                maxX = Math.Max(maxX, polygonPoints.ElementAt(i).X);
                minX = Math.Min(minX, polygonPoints.ElementAt(i).X);
                maxY = Math.Max(maxY, polygonPoints.ElementAt(i).Y);
                minY = Math.Min(minY, polygonPoints.ElementAt(i).Y);
            }

            var attemptTotal = 0;
            Random random = new Random();

            for (int i = 0; i < Configuration.randomPointNumber; i++)
            {
                int attempt = 0;
                while(true)
                {
                    attempt++;
                    PostgisPoint newTmpPoint = new PostgisPoint(random.NextDouble() * (maxX - minX) + minX, random.NextDouble() * (maxY - minY) + minY);

                    if (IsPointInsidePolygon(newTmpPoint, polygonPoints))
                    {
                        //Console.WriteLine(attempt);
                        attemptTotal += attempt;
                        result.Add(newTmpPoint);
                        break;
                    }
                }
            }

            //Console.WriteLine("avg attempt for each satisfied random point: " + attemptTotal * 1.0 / Configuration.randomPointNumber);
            Console.WriteLine("total random point number: " + Configuration.randomPointNumber);
            Console.WriteLine("total time: {0}\n", (System.DateTime.Now - startTime).TotalMilliseconds / 1000.0);
            return result;
        }

        /*
         * We use ray casting algorithm to check a point in the polygon, 
         * a horizontal ray start the point forward to the right side
         * if the ray cuts the polygon odd times the point is inside the polygon, ortherwise for even times the point is outside the polygon,
         * the algorithm loops through edges of the polygon, so the complexity is O(number of edges of polygon * number of random points
         * * number of attempts for each satisfied random point)
         */
        public bool IsPointInsidePolygon(PostgisPoint point, List<PostgisPoint> pointsList)
        {
            bool isInside = false;

            for(int i = 1; i < pointsList.Count; i++)
            {
                int check = DoesRayIntersectEdge(point, pointsList.ElementAt(i - 1), pointsList.ElementAt(i));

                // point lying on the edge
                if (check == 0)
                    return true;

                // ray cuts the polygon
                if (check == 1)
                    isInside = !isInside;
            }

            return isInside;
        }

        /*
         * Check if the ray from the point to the right side cuts the edge of 2 point a, b
         * -1 means no cut, 0 means the point lie on the edge, 1 means they intersect
         */
        public int DoesRayIntersectEdge(PostgisPoint point, PostgisPoint pointA, PostgisPoint pointB)
        {
            if (point.X > Math.Max(pointA.X, pointB.X))
                return -1;

            if (Math.Abs(point.Y - pointA.Y) < Esp || Math.Abs(point.Y - pointB.Y) < Esp)
            {
                point = new PostgisPoint(point.X, point.Y + Esp * 4);
            }

            if (pointA.Y < pointB.Y)
            {
                if (point.Y > pointB.Y || point.Y < pointA.Y)
                    return -1;
            }
            else
            {
                if (point.Y < pointB.Y || point.Y > pointA.Y)
                    return -1;
            }

            // if point belongs to the line equation of 2 points
            if (LineEquation(point, pointA, pointB) * LineEquation(point, pointA, pointB) <= Esp * Esp)
            {
                // point lying on the left of the edge, not lying on the edge, we dont count this special case, return -1, they intersected through
                if (point.X < Math.Min(pointA.X, pointB.X))
                    return -1;

                // point lying on the edge
                return 0;
            }

            PostgisPoint infinityRightMostPoint = new PostgisPoint(Double.MaxValue, point.Y);

            if (LineEquation(point, pointA, pointB) * LineEquation(infinityRightMostPoint, pointA, pointB) < 0)
                // 2 point are on 2 sides, the ray cuts the edge
                return 1;
            else
                return -1;
        }

        /*
         * The line equation is: (x - xa) / (xb - xa) = (y - ya) / (yb - ya)
         * 2 points are on the same side if the equation give the same sign
         */
        public double LineEquation(PostgisPoint point, PostgisPoint pointA, PostgisPoint pointB)
        {
            return ((point.X - pointB.X) * (pointA.Y - pointB.Y) - (point.Y - pointB.Y) * (point.X - pointB.X));
        }

        public double LineEquation(PostgisPoint point, Coordinate2D pointA, Coordinate2D pointB)
        {
            return ((point.X - pointB.X) * (pointA.Y - pointB.Y) - (point.Y - pointB.Y) * (point.X - pointB.X));
        }

        public bool Test_isPointsApart30km_TrueExpected(List<PostgisPoint> points)
        {
            for (int i = 0; i < points.Count; i++)
                for(int j = 0; j < points.Count; j++) if (i != j)
                {
                    if (DistanceTwoGeoPoint(points.ElementAt(i), points.ElementAt(j)) < Configuration.apartDistance)
                        return false;
                }

            return true;
        }

        public double DistanceTwoGeoPoint(PostgisPoint pointA, PostgisPoint pointB)
        {
            return DistanceTo(pointA.Y, pointA.X, pointB.Y, pointB.X, 'K');
        }

        public double DistanceTo(double lat1, double lon1, double lat2, double lon2, char unit = 'K')
        {
            double rlat1 = Math.PI * lat1 / 180;
            double rlat2 = Math.PI * lat2 / 180;
            double theta = lon1 - lon2;
            double rtheta = Math.PI * theta / 180;
            double dist = Math.Sin(rlat1) * Math.Sin(rlat2) + Math.Cos(rlat1)
                          * Math.Cos(rlat2) * Math.Cos(rtheta);

            dist = Math.Acos(dist);
            dist = dist * 180 / Math.PI;
            dist = dist * 60 * 1.1515;

            switch (unit)
            {
                case 'K': //Kilometers -> default
                    return dist * 1.609344;
                case 'N': //Nautical Miles 
                    return dist * 0.8684;
                case 'M': //Miles
                    return dist;
            }

            return dist;
        }
    }
}
