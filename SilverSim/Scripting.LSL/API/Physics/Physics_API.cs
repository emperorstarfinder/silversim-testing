﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.LSL.API.Physics
{
    [ScriptApiName("Physics")]
    [LSLImplementation]
    public class Physics_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        public Physics_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL)]
        public void llSetTorque(ScriptInstance Instance, Vector3 torque, int local)
        {
            lock (Instance)
            {
                IPhysicsObject physobj = Instance.Part.ObjectGroup.RootPart.PhysicsActor;
                if (null == physobj)
                {
                    Instance.ShoutError("Object has not physical properties");
                    return;
                }
                
                if (local != 0)
                {
                    physobj.AppliedTorque = torque / Instance.Part.ObjectGroup.GlobalRotation;
                }
                else
                {
                    physobj.AppliedTorque = torque;
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llSetForce(ScriptInstance Instance, Vector3 force, int local)
        {
            lock (Instance)
            {
                IPhysicsObject physobj = Instance.Part.ObjectGroup.RootPart.PhysicsActor;
                if (null == physobj)
                {
                    Instance.ShoutError("Object has not physical properties");
                    return;
                }

                if (local != 0)
                {
                    physobj.AppliedForce = force / Instance.Part.ObjectGroup.GlobalRotation;
                }
                else
                {
                    physobj.AppliedForce = force;
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llSetForceAndTorque(ScriptInstance Instance, Vector3 force, Vector3 torque, int local)
        {
            lock (Instance)
            {
                IPhysicsObject physobj = Instance.Part.ObjectGroup.RootPart.PhysicsActor;
                if (null == physobj)
                {
                    Instance.ShoutError("Object has not physical properties");
                    return;
                }

                if (force == Vector3.Zero || torque == Vector3.Zero)
                {
                    physobj.AppliedTorque = Vector3.Zero;
                    physobj.AppliedForce = Vector3.Zero;
                }
                else if(local != 0)
                {
                    physobj.AppliedForce = force / Instance.Part.ObjectGroup.GlobalRotation;
                    physobj.AppliedTorque = torque / Instance.Part.ObjectGroup.GlobalRotation;
                }
                else
                {
                    physobj.AppliedForce = force;
                    physobj.AppliedTorque = torque;
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llSetBuoyancy(ScriptInstance Instance, double buoyancy)
        {
            lock(Instance)
            {
                IPhysicsObject physobj = Instance.Part.ObjectGroup.RootPart.PhysicsActor;
                if (null == physobj)
                {
                    Instance.ShoutError("Object has not physical properties");
                    return;
                }

                physobj.Buoyancy = buoyancy;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llPushObject(ScriptInstance Instance, LSLKey target, Vector3 impulse, Vector3 ang_impulse, int local)
        {
#warning Implement llPushObject
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public void llApplyImpulse(ScriptInstance Instance, Vector3 momentum, int local)
        {
#warning Implement llApplyImpulse
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public void llApplyRotationalImpulse(ScriptInstance Instance, Vector3 ang_impulse, int local)
        {
#warning Implement llApplyRotationalImpulse
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public void llSetVelocity(ScriptInstance Instance, Vector3 velocity, int local)
        {
            lock (Instance)
            {
                /* we leave the physics check out here since it has an interesting use */
                if (local != 0)
                {
                    Instance.Part.ObjectGroup.Velocity = velocity / Instance.Part.ObjectGroup.GlobalRotation;
                }
                else
                {
                    Instance.Part.ObjectGroup.Velocity = velocity;
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llSetAngularVelocity(ScriptInstance Instance, Vector3 initial_omega, int local)
        {
            lock (Instance)
            {
                /* we leave the physics check out here since it has an interesting use */
                if (local != 0)
                {
                    Instance.Part.ObjectGroup.AngularVelocity = initial_omega / Instance.Part.ObjectGroup.GlobalRotation;
                }
                else
                {
                    Instance.Part.ObjectGroup.AngularVelocity = initial_omega;
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public AnArray llGetPhysicsMaterial(ScriptInstance Instance)
        {
            AnArray array = new AnArray();
            lock (Instance)
            {
                array.Add(Instance.Part.ObjectGroup.RootPart.PhysicsGravityMultiplier);
                array.Add(Instance.Part.ObjectGroup.RootPart.PhysicsRestitution);
                array.Add(Instance.Part.ObjectGroup.RootPart.PhysicsFriction);
                array.Add(Instance.Part.ObjectGroup.RootPart.PhysicsDensity);
                return array;
            }
        }

        const int DENSITY = 1;
        const int FRICTION = 2;
        const int RESTITUTION = 4;
        const int GRAVITY_MULTIPLIER = 8;

        [APILevel(APIFlags.LSL)]
        public void llSetPhysicsMaterial(ScriptInstance Instance, int mask, double gravity_multiplier, double restitution, double friction, double density)
        {
            lock (Instance)
            {
                if (0 != (mask & DENSITY))
                {
                    if (density < 1)
                    {
                        density = 1;
                    }
                    else if (density > 22587f)
                    {
                        density = 22587f;
                    }
                    Instance.Part.ObjectGroup.RootPart.PhysicsDensity = density;
                }
                if (0 != (mask & FRICTION))
                {
                    if (friction < 0)
                    {
                        friction = 0f;
                    }
                    else if (friction > 255f)
                    {
                        friction = 255f;
                    }
                    Instance.Part.ObjectGroup.RootPart.PhysicsFriction = friction;
                }
                if (0 != (mask & RESTITUTION))
                {
                    if (restitution < 0f)
                    {
                        restitution = 0f;
                    }
                    else if (restitution > 1f)
                    {
                        restitution = 1f;
                    }
                    Instance.Part.ObjectGroup.RootPart.PhysicsRestitution = restitution;
                }
                if (0 != (mask & GRAVITY_MULTIPLIER))
                {
                    if (gravity_multiplier < -1f)
                    {
                        gravity_multiplier = -1f;
                    }
                    else if (gravity_multiplier > 28f)
                    {
                        gravity_multiplier = 28f;
                    }
                    Instance.Part.ObjectGroup.RootPart.PhysicsGravityMultiplier = gravity_multiplier;
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llSetHoverHeight(ScriptInstance Instance, double height, int water, double tau)
        {
#warning Implement llSetHoverHeight
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public void llStopHover(ScriptInstance Instance)
        {
#warning Implement llStopHover
            throw new NotImplementedException();
        }
    }
}
