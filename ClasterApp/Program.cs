using System;
using System.Collections.Generic;
using System.IO;

namespace ClasterApp
{
    class Program
    {
        static void Main(string[] args)
        {
            /*** Настройки ***/
            
            //Говорим, что запятая в double - это точка
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            /*** Конец Настройки ***/


            /*** Некоторые важные переменные ***/

            String vStr;                                //нужно для чтения из файла
            string vPatch = "data.txt";                 //путь к файлу с данными
            double[][] rawData = new double[0][];       //пустой массив с нулевой размерностью. Будет расширяться динамически

            /*** Конец переменные ***/

            Console.WriteLine("/***********************************************/");
            Console.WriteLine("/                                               /");
            Console.WriteLine("/             Алгоритм k-срдених++              /");
            Console.WriteLine("/                                               /");
            Console.WriteLine("/              Автор: Белов Антон               /");
            Console.WriteLine("/           Студент гр. 8АПм-02-11оп            /");
            Console.WriteLine("/                                               /");
            Console.WriteLine("/***********************************************/");

            Console.WriteLine();

            try
            {
                StreamReader sr = new StreamReader("data.txt");

                while ((vStr = sr.ReadLine()) != null)
                {
                    Array.Resize(ref rawData, rawData.Length + 1);
                    rawData[rawData.Length - 1] = new double[] { Convert.ToDouble(vStr.Split(' ')[0]), Convert.ToDouble(vStr.Split(' ')[1]) };
                }
           
                       
                Console.Write("Введите количество кластеров: ");
                int numClusters = Convert.ToInt32(Console.ReadLine());
            
                //Отобразим что считали из файла
                ShowData(rawData, 1, true, true);

                Console.WriteLine("\nСтарт работы алгоритма...");
                int[] clustering = Cluster(rawData, numClusters, 0);

                Console.WriteLine("Алгоритм завершен!\n");

                Console.WriteLine("Присовение к кластеру:\n");
                ShowVector(clustering, true);

                Console.WriteLine("Итоговые данные:\n");
                ShowClustered(rawData, clustering, numClusters, 1);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: файл data.txt не найден в корневой папке с программой!");
            }

            Console.WriteLine("Для закрытия программы нажмите Enter...");
            Console.ReadLine();

        }

        public static int[] Cluster(double[][] rawData, int numClusters, int seed)
        {
            double[][] data = Normalized(rawData); // нормализация для исходных данных

            bool changed = true; // изменить хотя бы одно назначение кластера?
            bool success = true; // нет кластеров с нулевым счетом?

            double[][] means = InitMeans(numClusters, data, seed);

            int[] clustering = new int[data.Length];

            int maxCount = data.Length * 10;
            int ct = 0;
            while (changed == true && success == true && ct < maxCount)
            {
                changed = UpdateClustering(data, clustering, means); 
                success = UpdateMeans(data, clustering, means);
                ++ct;
            }
            return clustering;
        }

        private static double[][] InitMeans(int numClusters, double[][] data, int seed)
        {
            // выбираем k элементов данных в качестве начальных средств
            // случайным образом выбираем один элемент данных
            // цикл k-1 раз
            // вычисляем расстояние^2 от каждого элемента до ближайшего среднего
            // выбираем элемент данных с большим расстояние^2 в качестве следующего среднего
            // конец цикла

            double[][] means = MakeMatrix(numClusters, data[0].Length);

            List<int> used = new List<int>();

            Random rnd = new Random(seed);
            int idx = rnd.Next(0, data.Length);
            Array.Copy(data[idx], means[0], data[idx].Length);
            used.Add(idx);

            for (int k = 1; k < numClusters; ++k)
            {
                double[] dSquared = new double[data.Length]; 
                int newMean = -1; 
                for (int i = 0; i < data.Length; ++i)
                {
                  
                    if (used.Contains(i) == true) continue;

                    double[] distances = new double[k];
                    for (int j = 0; j < k; ++j)
                        distances[j] = Distance(data[i], means[k]);

                    int m = MinIndex(distances);
                  
                    dSquared[i] = distances[m] * distances[m];
                }

                double p = rnd.NextDouble();
                double sum = 0.0; 
                for (int i = 0; i < dSquared.Length; ++i)
                    sum += dSquared[i];
                double cumulative = 0.0;

                int ii = 0; 
                int sanity = 0;
                while (sanity < data.Length * 2)
                {
                    cumulative += dSquared[ii] / sum;
                    if (cumulative >= p && used.Contains(ii) == false)
                    {
                        newMean = ii;
                        used.Add(newMean);
                        break;
                    }
                    ++ii;
                    if (ii >= dSquared.Length) ii = 0;
                    ++sanity;
                }
 
                Array.Copy(data[newMean], means[k], data[newMean].Length);
            }

            return means;

        }

        private static double[][] Normalized(double[][] rawData)
        {
            // нормализуем необработанные данные путем вычисления (x - mean) / stddev
            // одна альтернатива это min-max:
            // v '= (v - min) / (max - min)

            double[][] result = new double[rawData.Length][];
            for (int i = 0; i < rawData.Length; ++i)
            {
                result[i] = new double[rawData[i].Length];
                Array.Copy(rawData[i], result[i], rawData[i].Length);
            }

            for (int j = 0; j < result[0].Length; ++j)
            {
                double colSum = 0.0;
                for (int i = 0; i < result.Length; ++i)
                    colSum += result[i][j];
                double mean = colSum / result.Length;
                double sum = 0.0;
                for (int i = 0; i < result.Length; ++i)
                    sum += (result[i][j] - mean) * (result[i][j] - mean);
                double sd = sum / result.Length;
                for (int i = 0; i < result.Length; ++i)
                    result[i][j] = (result[i][j] - mean) / sd;
            }
            return result;
        }

        private static double[][] MakeMatrix(int rows, int cols)
        {
            double[][] result = new double[rows][];
            for (int i = 0; i < rows; ++i)
                result[i] = new double[cols];
            return result;
        }

        private static bool UpdateMeans(double[][] data, int[] clustering, double[][] means)
        {
            // возвращает false, если есть кластер, которому не назначены кортежи
            // параметр означает, что means[][] действительно является параметром ссылки
            // проверка количества существующих кластеров

            int numClusters = means.Length;
            int[] clusterCounts = new int[numClusters];
            for (int i = 0; i < data.Length; ++i)
            {
                int cluster = clustering[i];
                ++clusterCounts[cluster];
            }

            for (int k = 0; k < numClusters; ++k)
                if (clusterCounts[k] == 0)
                    return false;

            // обновление, обнуление означает, что он может быть использован в качестве матрицы нуля
            for (int k = 0; k < means.Length; ++k)
                for (int j = 0; j < means[k].Length; ++j)
                    means[k][j] = 0.0;

            for (int i = 0; i < data.Length; ++i)
            {
                int cluster = clustering[i];
                for (int j = 0; j < data[i].Length; ++j)
                    means[cluster][j] += data[i][j];
            }

            for (int k = 0; k < means.Length; ++k)
                for (int j = 0; j < means[k].Length; ++j)
                    means[k][j] /= clusterCounts[k];
            return true;
        }

        private static bool UpdateClustering(double[][] data, int[] clustering,
          double[][] means)
        {
            // (пере) назначаем каждый кортеж кластеру (индекс ближайшего среднего)
            // возвращает false, если назначения кортежей не меняются ИЛИ
            // если переназначение приведет к кластеризации где
            // один или несколько кластеров не имеют кортежей.

            int numClusters = means.Length;
            bool changed = false;

            int[] newClustering = new int[clustering.Length];
            Array.Copy(clustering, newClustering, clustering.Length);

            double[] distances = new double[numClusters]; 

            for (int i = 0; i < data.Length; ++i) 
            {
                for (int k = 0; k < numClusters; ++k)
                    distances[k] = Distance(data[i], means[k]); 

                int newClusterID = MinIndex(distances);

                if (newClusterID != newClustering[i])
                {
                    changed = true;
                    newClustering[i] = newClusterID;
                }
            }

            if (changed == false)
                return false;

            // проверка предлагаемой кластеризации
            int[] clusterCounts = new int[numClusters];
            for (int i = 0; i < data.Length; ++i)
            {
                int cluster = newClustering[i];
                ++clusterCounts[cluster];
            }

            for (int k = 0; k < numClusters; ++k)
                if (clusterCounts[k] == 0)
                    return false; // кластеризация не прошла. clustering[][] не меняем

            Array.Copy(newClustering, clustering, newClustering.Length);
            return true;
        }
        
        // Евклидово расстояние между двумя векторами
        private static double Distance(double[] tuple, double[] mean)
        {
            double sumSquaredDiffs = 0.0;
            for (int j = 0; j < tuple.Length; ++j)
                sumSquaredDiffs += Math.Pow((tuple[j] - mean[j]), 2);
            return Math.Sqrt(sumSquaredDiffs);
        }
        
        //ищем индекс наименьшего значения массива
        private static int MinIndex(double[] distances)
        {
            int indexOfMin = 0;
            double smallDist = distances[0];
            for (int k = 0; k < distances.Length; ++k)
            {
                if (distances[k] < smallDist)
                {
                    smallDist = distances[k];
                    indexOfMin = k;
                }
            }
            return indexOfMin;
        }

        // отображаем набор считанных данных
        static void ShowData(double[][] data, int decimals,
          bool indices, bool newLine)
        {
            for (int i = 0; i < data.Length; ++i)
            {
                if (indices) Console.Write(i.ToString().PadLeft(3) + " ");
                for (int j = 0; j < data[i].Length; ++j)
                {
                    if (data[i][j] >= 0.0) Console.Write(" ");
                    Console.Write(data[i][j].ToString("F" + decimals) + " ");
                }
                Console.WriteLine("");
            }
            if (newLine) Console.WriteLine("");
        }

        // отображаем присовение кластеру
        static void ShowVector(int[] vector, bool newLine)
        {
            for (int i = 0; i < vector.Length; ++i)
                Console.Write(vector[i] + " ");
            if (newLine) Console.WriteLine("\n");
        }

        // отображаем присовение кластеру
        static void ShowVector(double[] vector, int decimals, bool newLine)
        {
            for (int i = 0; i < vector.Length; ++i)
                Console.Write(vector[i].ToString("F" + decimals) + " ");
            if (newLine) Console.WriteLine("\n");
        }

        // отображение кластеры
        static void ShowClustered(double[][] data, int[] clustering, int numClusters, int decimals)
        {
            for (int k = 0; k < numClusters; ++k)
            {
                Console.WriteLine("======= " + (k+1) + " кластер" + " =======");
                for (int i = 0; i < data.Length; ++i)
                {
                    int clusterID = clustering[i];
                    if (clusterID != k) continue;
                    Console.Write(i.ToString().PadLeft(3) + " ");
                    for (int j = 0; j < data[i].Length; ++j)
                    {
                        if (data[i][j] >= 0.0) Console.Write(" ");
                        Console.Write(data[i][j].ToString("F" + decimals) + " ");
                    }
                    Console.WriteLine("");
                }
                Console.WriteLine("=========================");
            }
        }
    }
}
