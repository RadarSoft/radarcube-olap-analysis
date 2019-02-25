using System.Collections.Generic;

namespace RadarSoft.RadarCube.ClientAgents
{
    /// <exclude />
    public class GenericMenu : List<GenericMenuItem>
    {
        public void AddSeparator()
        {
            if (Count > 0 && this[Count - 1].ActionType != GenericMenuActionType.Separator)
            {
                var s = new GenericMenuItem(GenericMenuActionType.Separator);
                Add(s);
            }
        }
    }
}