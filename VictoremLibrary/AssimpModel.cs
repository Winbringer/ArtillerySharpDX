using Assimp;
using Assimp.Configs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VictoremLibrary
{
 public   class AssimpModel
    {
        public Scene model;
       public AssimpModel(string File)
        {
            String fileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), File);

            using (AssimpContext importer = new AssimpContext())
            {
                NormalSmoothingAngleConfig config = new NormalSmoothingAngleConfig(66.0f);
                importer.SetConfig(config);
                using (LogStream logstream = new LogStream(delegate (String msg, String userData)
                 {
                     Console.WriteLine(msg);

                 }))
                {
                    logstream.Attach();

                     model = importer.ImportFile(fileName, PostProcessPreset.TargetRealTimeMaximumQuality);
                    
                    //TODO: Загрузить данные в мои собственные классы и структуры.                 
                }
            }
        }

    }
}
