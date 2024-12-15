using Dhs5.Utility.Databases;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Updates
{
    [Database("Update/Timelines", typeof(UpdateTimelineDatabaseElement))]
    public class UpdateTimelineDatabase : ScriptableDatabase<UpdateTimelineDatabase>
    {

    }
}
