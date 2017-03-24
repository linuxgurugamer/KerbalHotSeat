using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ConnectedLivingSpace;

namespace KerbalHotSeat
{

//    [KSPAddonFixedKHS(KSPAddon.Startup.Flight, false, typeof(KHSAddon))]
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KHSAddon : MonoBehaviour
    {
        // A struct to hold the combination of a part and a seat within it.
        public class PartSeat
        {
            public PartSeat(Part _part, int _seat)
            {
                this.part = _part;
                this.seat = _seat;
            }
            public Part part;
            public int seat;
        }

        private double lastBurn = 0;
        private double lastMovement = 0;
#if DEBUG
        private double minTimeBetweenMovements = 30;
#else
        private double minTimeBetweenMovements = 300;
#endif
        private System.Random random; // Random number generator
        private bool refreshPortraitsPending = false;
        
        public void Awake()
        {
            //Debug.Log("KHSAddon:Awake");
            random = new System.Random();
        }

        public void Start()
        {
            //Debug.Log("KHSAddon:Start");

            // On starting pretend that we have just had a movement. That way we will not have another one straight away.
            this.lastMovement = Planetarium.GetUniversalTime();

        }

         public void Update()
        {
            // Debug.Log("KHSAddon:Update");
        }

         public void FixedUpdate()
         {
             // Debug.Log("KHSAddon:FixedUpdate");

             // Have we scheduled a portrait refresh? if so do it and reset the flag. TODO does this work?
             if (this.refreshPortraitsPending)
             {
                 //Debug.Log("refreshing the portraits.");
                 FlightGlobals.ActiveVessel.SpawnCrew();
                 this.refreshPortraitsPending = false;
             }

             // Dont bother doing anything unless we are actually flying around in space
             if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.DOCKED
                 || FlightGlobals.ActiveVessel.situation == Vessel.Situations.ORBITING
                 || FlightGlobals.ActiveVessel.situation == Vessel.Situations.ESCAPING)
             {
                 //Debug.Log("flightstate: " + FlightGlobals.ActiveVessel.situation + " does allow kerbals to change seats");

                 // Don't allow kerbals to swap around while the craft is under acceleration
                 if (FlightGlobals.ActiveVessel.ctrlState.mainThrottle == 0.0) // TODO check that this is zero when in orbit. Is gravity included in this acceleration figure?
                 {
                     // Consider moving a kerbal from one seat to another.
                     if (this.lastMovement + this.minTimeBetweenMovements < Planetarium.GetUniversalTime()
                         && this.lastBurn + this.minTimeBetweenMovements < Planetarium.GetUniversalTime())
                     {
                         // A movement is possible.

                         double fixedDeltasInMinTimeBetweenMovements = this.minTimeBetweenMovements / (double)Time.fixedDeltaTime;
                         double randomValue = this.random.NextDouble() * fixedDeltasInMinTimeBetweenMovements;
                         //Debug.Log("fixedDeltasInMinTimeBetweenMovements: " + fixedDeltasInMinTimeBetweenMovements + " randomValue: " + randomValue);
                         if (randomValue < 1.0)
                         {
                             //Debug.Log("Time to try to move a kerbal");
                             ChooseAndMoveAKerbal();

                             this.lastMovement = Planetarium.GetUniversalTime();
                         }
                     }
                     else
                     {
                         //Debug.Log("Time since last movement: " + (Planetarium.GetUniversalTime() - this.lastMovement) + " does not allow for kerbals to change seats.");
                         //Debug.Log("Time since last burn: " + (Planetarium.GetUniversalTime() - this.lastBurn) + " does not allow for kerbals to change seats.");
                     }
                 }
                 else
                 {
                     //Debug.Log("throttle: " + FlightGlobals.ActiveVessel.ctrlState.mainThrottle + " does not allow kerbals to change seats");
                     this.lastBurn = Planetarium.GetUniversalTime();
                 }
             }
             else
             {
                 //Debug.Log("flightstate: " +FlightGlobals.ActiveVessel.situation + " does not allow kerbals to change seats");
             }

         }

         private void ChooseAndMoveAKerbal()
         {
             // Debug.Log("KHSAddon:ChooseAndMoveAKerbal");

             //Debug.Log("Before moving KerbalGUIManager.ActiveCrew.Count:" + KerbalGUIManager.ActiveCrew.Count);

             // If CLS is not installed then just bug out
             if (!CLSClient.CLSInstalled)
             {
                 Debug.LogWarning("Not moving kerbals as the CLS mod is not installed");
                 return;
             }

             ICLSVessel activeVessel = CLSClient.GetCLS().Vessel;

             if(activeVessel.Spaces.Count >0)
             {
                 List<ICLSSpace> crewedSpaces = new List<ICLSSpace>(activeVessel.Spaces.Count);

                 foreach (ICLSSpace s in activeVessel.Spaces)
                 {
                     if (s.Crew.Count > 0)
                     {
                         crewedSpaces.Add(s);
                     }
                 }

                 // now we have got a list of crewed spaces - pick one at random
                 if (crewedSpaces.Count > 0)
                 {
                     ICLSSpace pickedSpace = crewedSpaces[this.random.Next(crewedSpaces.Count)];
                     
                     // Now pick one of the crew in that space at random!
                     
                     ICLSKerbal pickedKerbel = pickedSpace.Crew[this.random.Next(pickedSpace.Crew.Count)];

                     // Move move the kerbal somewhere else
                     MoveKerbal(pickedKerbel);
                 }
             }

             //Debug.Log("After moving KerbalGUIManager.ActiveCrew.Count:" + KerbalGUIManager.ActiveCrew.Count);

         }

         private void MoveKerbal(ICLSKerbal k)
         {
             // Debug.Log("MoveKerbal");
             // If there is only one seat in this space, then our friend is not going anywhere!
             ICLSPart temppart = k.Part;
             ICLSSpace s = temppart.Space;
             
             if (null == s)
             {
                 Debug.LogWarning("space is null :(");
             }

             int maxCrew = s.MaxCrew;

             if (k.Part.Space.MaxCrew > 1)
             {
                 //Debug.Log("vessel has more than 1 seat");

                 List<PartSeat> listSeats = new List<PartSeat>();

                 // There are other seats in the space that our chosen kerbal is in. Pick one of them at random, and then we can arrange the swap.
                 foreach (ICLSPart p in k.Part.Space.Parts)
                 {
                     for (int counter = 0; counter < p.Part.CrewCapacity; counter++)
                     {
                         //Debug.Log("Adding " + p.partInfo.title + " seat "+counter);
                         listSeats.Add(new PartSeat(p.Part, counter));
                     }
                 }

                 // Now we have a list of all the possible places we could move the kerbal to - choose one of them.

                 int choosenLocation = this.random.Next(listSeats.Count);

                 // TODO remove debugging
                 //Debug.Log("choosen location: " + choosenLocation);

                 // Do the swap
                 bool kerbalsSwapped = SwapKerbals(k, listSeats[choosenLocation].part, listSeats[choosenLocation].seat);
             }
             else
             {
                 //Debug.Log("Unable to move kerbal as there is only one seat in this vessel!");
             }
         }

         private bool SwapKerbals(ICLSKerbal k, Part targetPart, int targetSeatIdx)
         {
             //Debug.Log("SwapKerbals");

             if (k.Kerbal.seat.part == targetPart && k.Kerbal.seatIdx == targetSeatIdx)
             {
                 // Nothing to do because the choosen target seat is where the kerbal is 

                 //Debug.Log("Attempted to move kerbal to the seat he is already sitting in!");

                 return false;
             }
             else
             {
                 //Debug.Log("moving keral to somewhere other than his own seat");
                 ProtoCrewMember sourceKerbal = k.Kerbal;
                 // Find out if a kerbal is already sitting in the target seat
                 // ProtoCrewMember targetKerbal = targetSeat.part.vessel.GetVesselCrew().Find(c => (c.seat.part == targetSeat.part) && (c.seatIdx == targetSeat.seat));

                 Part sourcePart = k.Part.Part;
                 ProtoCrewMember targetKerbal = targetPart.internalModel.seats[targetSeatIdx].crew; // TODO I am unsure what will happen if a pod does not have an internal model.
                 int sourceSeatIdx = sourceKerbal.seatIdx;

                 // TODO remove debugging
                 /*
                 {
                     Debug.Log("sourceKerbel: " + sourceKerbal.name);
                     Debug.Log("sourcePart:" + sourcePart.partInfo.title);
                     Debug.Log("sourceSeatIdx:" + sourceSeatIdx);

                     if (null == targetKerbal)
                     {
                         Debug.Log("targetKerbal: null");
                     }
                     else
                     {
                         Debug.Log("targetKerbal: " + targetKerbal.name);
                     }
                     Debug.Log("targetPart:" + targetPart.partInfo.title);
                     Debug.Log("targetSeatIdx:" + targetSeatIdx);
                 }
                 */

                 // Remove the kerbal(s) from their current seat(s)
                 sourcePart.RemoveCrewmember(sourceKerbal);
                 if (null != targetKerbal)
                 {
                     targetPart.RemoveCrewmember(targetKerbal);
                 }

                 // Add the source kerbal to his new seat
                 targetPart.AddCrewmemberAt(sourceKerbal, targetSeatIdx);
                 if (null != sourceKerbal.seat)
                 {
                     sourceKerbal.seat.SpawnCrew();
                 }
                 else
                 {
                     Debug.LogError("sourceKerbal (" + sourceKerbal.name +") does not have a seat!");
                 }

                 // Add the target kerbal to his new seat (if there is one)
                 if (null != targetKerbal)
                 {
                     sourcePart.AddCrewmemberAt(targetKerbal, sourceSeatIdx);
                     if (null != targetKerbal.seat)
                     {
                         targetKerbal.seat.SpawnCrew();
                     }
                     else
                     {
                         Debug.LogError("targetKerbal (" + targetKerbal.name + ") does not have a seat!");
                     }
                 }

                 // Fire the vessel change event.In particular CLS will pick up on this and rebuild its understanding of what is what.
                 GameEvents.onVesselChange.Fire(FlightGlobals.ActiveVessel);
                 //FlightGlobals.ActiveVessel.SpawnCrew(); // This does not seem to do the trick, so instead we set a flag and try to respawn on the next Update.
                 this.refreshPortraitsPending = true;
                 // Fire the Vessel Was Changed event in an attempt to update the GUI
                 //GameEvents.onVesselWasModified.Fire(FlightGlobals.ActiveVessel);

                 return true;
             }

         }

         public void OnDestroy()
         {
            //Debug.Log("KHSAddon::OnDestroy");
            
         }
    }
}
