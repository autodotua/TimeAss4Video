using FzLib.DataStorage.Serialization;
using System.Collections.ObjectModel;
using System.IO;

namespace TimeAss4Video
{
    public class Config : JsonSerializationBase
    {
        private static Config instance;

        public static Config Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = OpenOrCreate<Config>();
                }
                return instance;
            }
        }
    }
}