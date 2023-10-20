using System.Collections.Generic;
using System.Linq;

namespace Unity.Muse.Common
{
    internal static class ArtifactExtension
    {
        /// <summary>
        /// Utility method to get operators of a certain type from an artifact.
        /// </summary>
        /// <param name="artifact">Artifact to get operators from.</param>
        /// <typeparam name="T">Type of operators to get.</typeparam>
        /// <returns></returns>
        public static T GetOperator<T>(this Artifact artifact) where T: class, IOperator
        {
            return artifact == null ? null : artifact.GetOperators().GetOperator<T>();
        }

        /// <summary>
        /// Utility method to get operators of a certain type from a list of operators.
        /// </summary>
        /// <param name="operators">The operators to filter.</param>
        /// <typeparam name="T">Type of operators to get.</typeparam>
        /// <returns></returns>
        public static T GetOperator<T>(this IEnumerable<IOperator> operators) where T: class, IOperator
        {
            return operators?.FirstOrDefault(x => x.GetType() == typeof(T)) as T;
        }
    }
}
