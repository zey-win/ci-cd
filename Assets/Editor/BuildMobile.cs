using System.Collections.Generic;
using UnityEditor;

namespace ScriptedBuild
{
    public class BuildMobile : AbstractBuild
    {
        public new static void BuildOptions()
        {
            SetScenes(new List<string>
            {
                "Assets/Scenes/PlinkoMobile.unity",
            });

            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.WebGL.template = "PROJECT:Dbd";

            AbstractBuild.BuildOptions();
        }
    }
}