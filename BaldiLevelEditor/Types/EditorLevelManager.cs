using MTM101BaldAPI.Reflection;
using System;
using System.Collections.Generic;
using System.Text;

namespace BaldiLevelEditor.Types
{
    public class EditorLevelManager : MainGameManager
    {
        public override void Initialize()
        {
            gameObject.SetActive(true);
            base.Initialize();
        }

        public override void LoadNextLevel()
        {
            Singleton<CoreGameManager>.Instance.Quit();
        }
    }
}
