using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldBoxModdingToolChain.Utils
{
    public static class Constants
    {
        public static string TraitsBoilerplate = 
                "using System;\r\n" +
                "using UnityEngine;\r\n" +
                "using ReflectionUtility;\r\n" +
                "using System.Collections.Generic;\r\n" +
                "using static UnityEngine.GraphicsBuffer;\r\n" +
                "using Amazon.Runtime.Internal.Transform;\r\n" +
                "using UnityEngine.Tilemaps;\r\n" +
                "using System.Linq;\r\n" +
                @"
                //Put the name of your mod here
                namespace FillThisOut
                { 
                    class Traits
                    {
                        public static void init()
                        {
                            //Make your trait objects here
                        }
                        
                        //How you keep localization
                        public static void addTraitToLocalizedLibrary(string id, string description)
                        {
                            string language = Reflection.GetField(LocalizedTextManager.instance.GetType(), LocalizedTextManager.instance, ""language"") as string;
                            Dictionary<string, string> localizedText = Reflection.GetField(LocalizedTextManager.instance.GetType(), LocalizedTextManager.instance, ""localizedText"") as Dictionary<string, string>;
                            localizedText.Add(""trait_"" + id, id);
                            localizedText.Add(""trait_"" + id + ""_info"", description);
                        }
                    }
                }";

        public static string UnitsBoilerplate =
            "using NCMS.Utils;\r\n" +
            "using ReflectionUtility;\r\n" +
            "public static void init()\r\n" +
            "        {\r\n" +
            "            //you can/should make your kingdoms here if you are going to make any"+
            "            loadAssets();\r\n" +
            "        }"+
            "public static void loadAssets()\r\n" +
            "        {" +
            "           //put units here" +
            "        }";

        public static string StatusesBoilerplate =
            "using System.Collections.Generic;\r\n" +
            "using ReflectionUtility;\r\n" +
            "using SleekRender;\r\n" +
            "using UnityEngine;\r\n" +
            "namespace FillThisOut\r\n" +
            "{\r\n" +
            "    class Statuses\r\n" +
            "    {\r\n" +
            "        public static void init()\r\n" +
            "        {\r\n" +
            "           //Initialize your statuses here" +
            "        }\r\n" +
            "        public static void localizeStatus(string id, string name, string description)\r\n" +
            "        {\r\n" +
            "            Dictionary<string, string> localizedText = Reflection.GetField(LocalizedTextManager.instance.GetType(), LocalizedTextManager.instance, \"localizedText\") as Dictionary<string, string>;\r\n" +
            "            localizedText.Add(name, id);\r\n" +
            "            localizedText.Add(description, description);\r\n" +
            "        }" +
            "    }\r\n" +
            "}";

        public static string ItemsBoilerplate =
            "using System;\r\n" +
            "using UnityEngine;\r\n" +
            "using ReflectionUtility;\r\n" +
            "using System.Collections.Generic;\r\n" +
            "using HarmonyLib;\r\n" +
            "using NCMS.Utils;\r\n" +
            "namespace FillThisOut\r\n" +
            "{\r\n" +
            "    class Items\r\n" +
            "    {\r\n" +
            "        public static void init()\r\n" +
            "        {\n" +
            "           //make game items here\n" +
            "        }\n" +
            "   }\n" +
            "}";
        public static string EffectsBoilerplate =
            "namespace GodsAndPantheons\r\n" +
            "{" +
            "   class NewEffects : MonoBehaviour\r\n" +
            "    {\r\n" +
            "        public static void init()\r\n" +
            "        {\r\n" +
            "            loadEffects();\r\n" +
            "        }\r\n" +
            "       private static void loadEffects()\r\n" +
            "       {\n\r" +
            "       }\r\n" +
            "     }\r\n" +
            "}";
    }
}
