using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UltraLiteDB;
using Unity.Muse.Common.Cache.LiteDb;
using BsonMapper = UltraLiteDB.BsonMapper;
using Object = UnityEngine.Object;
using Query = UltraLiteDB.Query;

namespace Unity.Muse.Common
{
    public class WebArtifactCache : BaseArtifactCache
    {
        readonly string k_FileStreamPath = $"{Application.persistentDataPath}/{k_DatabaseName}";
        const string k_DatabaseName = "ArtifactCache.db";
        const string k_ArtifactCollectionName = "artifacts";

        UltraLiteDatabase m_Database;
        UltraLiteDatabase db => m_Database ??= InitDb();
        UltraLiteCollection<ArtifactDatabaseObject> m_Collection;
        UltraLiteCollection<ArtifactDatabaseObject> collection => m_Collection ??= InitCollection();

        public override void Initialize()
        {
            BsonMapper.Global.RegisterType<Artifact>(
                serialize: (artifact) => JsonUtility.ToJson(artifact),
                deserialize: (artifact) => JsonUtility.FromJson<Artifact<Texture2D>>(artifact.AsString)
            );
        }

        UltraLiteDatabase InitDb()
        {
            using var fs = new FileStream(k_FileStreamPath, FileMode.OpenOrCreate);
            m_Database = new UltraLiteDatabase(fs);
            return m_Database;
        }

        UltraLiteCollection<ArtifactDatabaseObject> InitCollection()
        {
            var result = db.GetCollection<ArtifactDatabaseObject>(k_ArtifactCollectionName);
            result.EnsureIndex("Guid");
            return result;
        }

        /// <summary>
        /// Dispose of the database so that it's content is saved to disk.
        /// </summary>
        public override void Dispose()
        {
            db.Dispose();

            m_Database = null;
            m_Collection = null;
        }

        public override void Clear()
        {
            db.DropCollection(k_ArtifactCollectionName);
        }

        public override bool IsInCache(Artifact artifact)
        {
            try
            {
                var query = Query.Where("Guid", value => value.AsString == artifact.Guid);
                return collection.FindOne(query) != null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Error accessing cache, dropping collection to try to save issues.\nerror: " + e.Message);
                Clear();
                return false;
            }
        }

        public override void Write(Artifact artifact, byte[] value)
        {
            try
            {
                var query = Query.Where("Guid", value => value.AsString == artifact.Guid);
                var artifactObject = collection.FindOne(query) ?? new ArtifactDatabaseObject(artifact, value);

                collection.Upsert(artifactObject);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Error writing cache, dropping collection to try to save issues.\nerror: " + e.Message);
                Clear();
            }
        }

        public override Object Read(Artifact artifact)
        {
            try
            {
                var dbObject = GetArtifactObject(artifact);
                switch (dbObject.FileExtension)
                {
                    case "png":
                    {
                        var texture = new Texture2D(2, 2);
                        texture.LoadImage(dbObject.RawData);

                        return texture;
                    }

                    default:
                        throw new NotImplementedException();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Error reading cache, dropping collection to try to save issues.\nerror: " + e.Message);
                Clear();
                return null;
            }
        }

        public override byte[] ReadRawData(Artifact artifact)
        {
            return GetArtifactObject(artifact)?.RawData;
        }

        public override void Prune()
        {
            try
            {
                var query = Query.Where("CreatedDate", value => DateTime.Now - value.AsDateTime > TimeSpan.FromDays(10));
                var colArtifact = collection.Find(query);

                foreach (var artifact in colArtifact)
                {
                    var deleteQuery = Query.Where("Guid", value => value.AsString == artifact.Guid);
                    collection.Delete(deleteQuery);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Error pruning cache, dropping collection to try to save issues.\nerror: " + e.Message);
                Clear();
            }
        }

        public override void Delete(Artifact artifact)
        {
           DeleteMany(new []{artifact});
        }

        public override void DeleteMany(IEnumerable<Artifact> artifacts)
        {
            try
            {
                foreach (var artifact in artifacts)
                {
                    var query = Query.Where("Guid", value => value.AsString == artifact.Guid);
                    collection.Delete(query);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Error deleting element(s) in cache, dropping collection to try to save issues.\nerror: " + e.Message);
                Clear();
            }
        }

        ArtifactDatabaseObject GetArtifactObject(Artifact artifact)
        {
            try
            {
                var query = Query.Where("Guid", value => value.AsString == artifact.Guid);
                var colArtifact = collection.FindOne(query);

                return colArtifact;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Error reading cache, dropping collection to try to save issues.\nerror: " + e.Message);
                Clear();
                return null;
            }
        }
    }
}
