using System;

// K-means clustering. ('Lloyd's algorithm')
namespace Nyerguds.Util
{
    /// <summary>
    /// Generic implementation of K-means clustering.
    /// The object type for the data input and the average can be freely chosen.
    /// The Normalize and ClearMeans function are optional, for your own convenience.
    /// The CalculateClusterAverage and CalculateDistance need to be implemented.
    /// </summary>
    /// <typeparam name="Tdata">Type of the input data.</typeparam>
    /// <typeparam name="Uavg">Type of the averages data.</typeparam>
    public abstract class KMeans<Tdata,Uavg>
    {

        protected virtual Tdata[] Normalize(Tdata[] rawData) { return rawData; }
        protected virtual void ClearMeans(Uavg[] means, int clusterNumber) { means[clusterNumber] = default(Uavg); }
        protected abstract Uavg CalculateClusterAverage(Tdata[] subData);
        protected abstract double CalculateDistance(Tdata dataEntry, Uavg mean);

        /// <summary>
        /// Clusters the given data into the requested number of clusters.
        /// </summary>
        /// <param name="rawData">Array of data items.</param>
        /// <param name="numClusters">Number of clusters.</param>
        /// <param name="means">The final calculated means for each cluster.</param>
        /// <returns>An array indicating in which cluster each data item was sorted.</returns>
        public int[] Cluster(Tdata[] rawData, uint numClusters, out Uavg[] means)
        {
            if (rawData == null)
                throw new ArgumentNullException("rawData", "Data cannot be null.");
            if (numClusters == 0)
                throw new ArgumentOutOfRangeException("numClusters", "Cannot cluster into 0 clusters.");
            if (numClusters >= rawData.Length)
                throw new ArgumentOutOfRangeException("numClusters", "Amount of requested clusters is greater than or equal to the data length. No meaningful clustering can be performed.");

            // k-means clustering
            // index of return is tuple ID, cell is cluster ID
            // ex: [2 1 0 0 2 2] means tuple 0 is cluster 2, tuple 1 is cluster 1, tuple 2 is cluster 0, tuple 3 is cluster 0, etc.
            // an alternative clustering DS to save space is to use the .NET BitArray class
            Tdata[] data = this.Normalize(rawData); // so large values don't dominate

            bool changed = true; // was there a change in at least one cluster assignment?
            bool success = true; // were all means able to be computed? (no zero-count clusters)

            // init clustering[] to get things started
            // an alternative is to initialize means to randomly selected tuples
            // then the processing loop is
            // loop
            //    update clustering
            //    update means
            // end loop
            //clustering == array that determines which index in the original data belongs in which cluster.
            int[] clustering = this.InitClustering(data.Length, numClusters, 0); // semi-random initialization
            // Array in which to store the means of each cluster.
            // For our purpose, this will store the palette for each cluster.
            means = new Uavg[numClusters];

            int maxCount = data.Length * 10; // sanity check
            int ct = 0;
            while (changed && success && ct < maxCount)
            {
                ct++; // k-means typically converges very quickly
                success = this.UpdateMeans(data, clustering, means); // compute new cluster means if possible. no effect if fail
                changed = this.UpdateClustering(data, clustering, means); // (re)assign tuples to clusters. no effect if fail
            }
            // consider adding means[][] as an out parameter - the final means could be computed
            // the final means are useful in some scenarios (e.g., discretization and RBF centroids)
            // and even though you can compute final means from final clustering, in some cases it
            // makes sense to return the means (at the expense of some method signature uglinesss)
            //
            // another alternative is to return, as an out parameter, some measure of cluster goodness
            // such as the average distance between cluster means, or the average distance between tuples in
            // a cluster, or a weighted combination of both
            return clustering;
        }

        private int[] InitClustering(int numTuples, uint numClusters, int randomSeed)
        {
            // init clustering semi-randomly (at least one tuple in each cluster)
            // consider alternatives, especially k-means++ initialization,
            // or instead of randomly assigning each tuple to a cluster, pick
            // numClusters of the tuples as initial centroids/means then use
            // those means to assign each tuple to an initial cluster.
            Random random = new Random(randomSeed);
            int[] clustering = new int[numTuples];
            for (int i = 0; i < numClusters; ++i) // make sure each cluster has at least one tuple
                clustering[i] = i;
            int clusteringLength = clustering.Length;
            for (uint i = numClusters; i < clusteringLength; ++i)
                clustering[i] = random.Next(0, (int)numClusters); // other assignments random
            return clustering;
        }

        private bool UpdateMeans(Tdata[] data, int[] clustering, Uavg[] means)
        {
            // returns false if there is a cluster that has no tuples assigned to it
            // parameter means[][] is really a ref parameter

            // check existing cluster counts
            // can omit this check if InitClustering and UpdateClustering
            // both guarantee at least one tuple in each cluster (usually true)
            int numClusters = means.Length;
            int numData = data.Length;
            int[] clusterCounts = new int[numClusters];
            for (int i = 0; i < numData; ++i)
            {
                int cluster = clustering[i];
                clusterCounts[cluster]++;
            }

            for (int k = 0; k < numClusters; ++k)
                if (clusterCounts[k] == 0)
                    return false; // Bad clustering. No change to means[][]

            // update, zero-out means so it can be used as scratch matrix
            for (int k = 0; k < numClusters; ++k)
                this.ClearMeans(means, k);

            for (int k = 0; k < numClusters; ++k)
            {
                int count = 0;
                for (int i = 0; i < numData; ++i)
                    if (clustering[i] == k)
                        count++;
                Tdata[] subData = new Tdata[count];
                count = 0;
                for (int i = 0; i < numData; ++i)
                    if (clustering[i] == k)
                        subData[count++] = data[i];
                means[k] = this.CalculateClusterAverage(subData);
            }
            return true;
        }

        private bool UpdateClustering(Tdata[] data, int[] clustering, Uavg[] means)
        {
            // (re)assign each tuple to a cluster (closest mean)
            // returns false if no tuple assignments change OR
            // if the reassignment would result in a clustering where
            // one or more clusters have no tuples.

            int numClusters = means.Length;
            bool changed = false;

            int clusteringLength = clustering.Length;
            int[] newClustering = new int[clusteringLength]; // proposed result
            Array.Copy(clustering, newClustering, clusteringLength);

            double[] distances = new double[numClusters]; // distances from curr tuple to each mean
            int dataLength = data.Length;
            for (int i = 0; i < dataLength; ++i) // walk thru each tuple
            {
                for (int k = 0; k < numClusters; ++k)
                    distances[k] = this.CalculateDistance(data[i], means[k]); // compute distances from curr tuple to all k means

                int newClusterID = MinIndex(distances); // find closest mean ID
                if (newClusterID == newClustering[i])
                    continue;
                changed = true;
                newClustering[i] = newClusterID; // update
            }

            if (changed == false)
                return false; // no change so bail and don't update clustering[][]

            // check proposed clustering[] cluster counts
            int[] clusterCounts = new int[numClusters];
            for (int i = 0; i < dataLength; ++i)
            {
                int cluster = newClustering[i];
                ++clusterCounts[cluster];
            }

            for (int k = 0; k < numClusters; ++k)
                if (clusterCounts[k] == 0)
                    return false; // bad clustering. no change to clustering[][]

            Array.Copy(newClustering, clustering, newClustering.Length); // update
            return true; // good clustering and at least one change
        }

        private static int MinIndex(double[] distances)
        {
            // index of smallest value in array
            // helper for UpdateClustering()
            int indexOfMin = 0;
            double smallDist = distances[0];
            int distancesCount = distances.Length;
            for (int k = 0; k < distancesCount; ++k)
            {
                if (!(distances[k] < smallDist))
                    continue;
                smallDist = distances[k];
                indexOfMin = k;
            }
            return indexOfMin;
        }

    }
}
