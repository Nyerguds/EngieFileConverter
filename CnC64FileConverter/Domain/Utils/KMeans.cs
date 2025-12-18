using System;
using System.Linq;

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
        protected virtual void ClearMeans(Uavg[] means, Int32 clusterNumber) { means[clusterNumber] = default(Uavg); }
        protected abstract Uavg CalculateClusterAverage(Tdata[] subData);
        protected abstract Double CalculateDistance(Tdata dataEntry, Uavg mean);

        /// <summary>
        /// Clusters the given data into the requested number of clusters.
        /// </summary>
        /// <param name="rawData">Array of data items.</param>
        /// <param name="numClusters">Number of clusters.</param>
        /// <param name="means">The final calculated means for each cluster.</param>
        /// <returns>An array indicating in which cluster each data item was sorted.</returns>
        public Int32[] Cluster(Tdata[] rawData, UInt32 numClusters, out Uavg[] means)
        {
            if (rawData == null)
                throw new ArgumentNullException("rawData", "Data cannot be null!");
            if (numClusters == 0)
                throw new ArgumentOutOfRangeException("numClusters", "Cannot cluster into 0 clusters!");
            if (numClusters >= rawData.Length)
                throw new ArgumentOutOfRangeException("numClusters", "Amount of requested clusters is greater than or equal to the data length. No meaningful clustering can be performed.");

            // k-means clustering
            // index of return is tuple ID, cell is cluster ID
            // ex: [2 1 0 0 2 2] means tuple 0 is cluster 2, tuple 1 is cluster 1, tuple 2 is cluster 0, tuple 3 is cluster 0, etc.
            // an alternative clustering DS to save space is to use the .NET BitArray class
            Tdata[] data = this.Normalize(rawData); // so large values don't dominate

            Boolean changed = true; // was there a change in at least one cluster assignment?
            Boolean success = true; // were all means able to be computed? (no zero-count clusters)

            // init clustering[] to get things started
            // an alternative is to initialize means to randomly selected tuples
            // then the processing loop is
            // loop
            //    update clustering
            //    update means
            // end loop
            //clustering == array that determines which index in the original data belongs in which cluster.
            Int32[] clustering = InitClustering(data.Length, numClusters, 0); // semi-random initialization
            // Array in which to store the means of each cluster.
            // For our purpose, this will store the palette for each cluster.
            means = new Uavg[numClusters];

            Int32 maxCount = data.Length * 10; // sanity check
            Int32 ct = 0;
            while (changed && success && ct < maxCount)
            {
                ct++; // k-means typically converges very quickly
                success = UpdateMeans(data, clustering, means); // compute new cluster means if possible. no effect if fail
                changed = UpdateClustering(data, clustering, means); // (re)assign tuples to clusters. no effect if fail
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

        private Int32[] InitClustering(Int32 numTuples, UInt32 numClusters, Int32 randomSeed)
        {
            // init clustering semi-randomly (at least one tuple in each cluster)
            // consider alternatives, especially k-means++ initialization,
            // or instead of randomly assigning each tuple to a cluster, pick
            // numClusters of the tuples as initial centroids/means then use
            // those means to assign each tuple to an initial cluster.
            Random random = new Random(randomSeed);
            Int32[] clustering = new Int32[numTuples];
            for (Int32 i = 0; i < numClusters; ++i) // make sure each cluster has at least one tuple
                clustering[i] = i;
            for (UInt32 i = numClusters; i < clustering.Length; ++i)
                clustering[i] = random.Next(0, (Int32)numClusters); // other assignments random
            return clustering;
        }

        private Boolean UpdateMeans(Tdata[] data, Int32[] clustering, Uavg[] means)
        {
            // returns false if there is a cluster that has no tuples assigned to it
            // parameter means[][] is really a ref parameter

            // check existing cluster counts
            // can omit this check if InitClustering and UpdateClustering
            // both guarantee at least one tuple in each cluster (usually true)
            Int32 numClusters = means.Length;
            Int32 numData = data.Length;
            Int32[] clusterCounts = new Int32[numClusters];
            for (Int32 i = 0; i < numData; i++)
            {
                Int32 cluster = clustering[i];
                clusterCounts[cluster]++;
            }

            for (Int32 k = 0; k < numClusters; k++)
                if (clusterCounts[k] == 0)
                    return false; // Bad clustering. No change to means[][]

            // update, zero-out means so it can be used as scratch matrix
            for (Int32 k = 0; k < numClusters; ++k)
                ClearMeans(means, k);

            for (Int32 k = 0; k < numClusters; k++)
            {
                Int32 count = 0;
                for (Int32 i = 0; i < numData; ++i)
                    if (clustering[i] == k)
                        count++;
                Tdata[] subData = new Tdata[count];
                count = 0;
                for (Int32 i = 0; i < data.Length; ++i)
                    if (clustering[i] == k)
                        subData[count++] = data[i];
                means[k] = CalculateClusterAverage(subData);
            }
            return true;
        }

        private Boolean UpdateClustering(Tdata[] data, Int32[] clustering, Uavg[] means)
        {
            // (re)assign each tuple to a cluster (closest mean)
            // returns false if no tuple assignments change OR
            // if the reassignment would result in a clustering where
            // one or more clusters have no tuples.

            Int32 numClusters = means.Length;
            Boolean changed = false;

            Int32[] newClustering = new Int32[clustering.Length]; // proposed result
            Array.Copy(clustering, newClustering, clustering.Length);

            Double[] distances = new Double[numClusters]; // distances from curr tuple to each mean

            for (Int32 i = 0; i < data.Length; ++i) // walk thru each tuple
            {
                for (Int32 k = 0; k < numClusters; k++)
                    distances[k] = CalculateDistance(data[i], means[k]); // compute distances from curr tuple to all k means

                Int32 newClusterID = MinIndex(distances); // find closest mean ID
                if (newClusterID == newClustering[i])
                    continue;
                changed = true;
                newClustering[i] = newClusterID; // update
            }

            if (changed == false)
                return false; // no change so bail and don't update clustering[][]

            // check proposed clustering[] cluster counts
            Int32[] clusterCounts = new Int32[numClusters];
            for (Int32 i = 0; i < data.Length; ++i)
            {
                Int32 cluster = newClustering[i];
                ++clusterCounts[cluster];
            }

            for (Int32 k = 0; k < numClusters; ++k)
                if (clusterCounts[k] == 0)
                    return false; // bad clustering. no change to clustering[][]

            Array.Copy(newClustering, clustering, newClustering.Length); // update
            return true; // good clustering and at least one change
        }

        private static Int32 MinIndex(Double[] distances)
        {
            // index of smallest value in array
            // helper for UpdateClustering()
            Int32 indexOfMin = 0;
            Double smallDist = distances[0];
            for (Int32 k = 0; k < distances.Length; ++k)
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
