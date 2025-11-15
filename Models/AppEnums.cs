using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkstationJobSimulator.Models
{
    public enum WorkstationState
    {
        Idle,
        Processing
    }
    public enum PowerMode
    {
        Off = 0,
        GridOnly,
        GridAndCharge,
        Battery
    }

    public enum BatteryStatus
    {
        Ok = 0,
        Cutoff
    }

    public enum AmplifierStatus
    {
        Off = 0,
        On,
        Fault
    }

    public enum NetState
    {
        Normal = 0,
        HighLatency,
        Offline,
        ControllerFailure
    }

    public enum NetworkChannel
    {
        Reserve = 0,
        Main = 1
    }
}
