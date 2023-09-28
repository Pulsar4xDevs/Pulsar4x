using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Pulsar4X.Components
{
    public abstract class ComponentTreeHeirarchyAbilityState : ComponentAbilityState
    {
        public ComponentTreeHeirarchyAbilityState ParentState { get; private set; }
        public List<ComponentTreeHeirarchyAbilityState> ChildrenStates { get; private set; } = new List<ComponentTreeHeirarchyAbilityState>();



        public ComponentTreeHeirarchyAbilityState(ComponentInstance componentInstance) : base(componentInstance)
        {

        }

        /// <summary>
        /// Sets the parent of this. (no need to set this as a child on the parent)
        /// </summary>
        /// <param name="newParent"></param>
        public void SetParent(ComponentTreeHeirarchyAbilityState newParent)
        {
            if (ParentState != null)
            {
                ParentState.ChildrenStates.Remove(this);
            }

            ParentState = newParent;
            if(newParent != null)
                ParentState.ChildrenStates.Add(this);
        }

        /// <summary>
        /// Clears any exsisting children and sets children to the given list (no need to set parent on children seperately)
        /// </summary>
        /// <param name="children"></param>
        public void SetChildren(ComponentTreeHeirarchyAbilityState[] children)
        {
            var oldChilders = new List<ComponentTreeHeirarchyAbilityState>(ChildrenStates);
            ChildrenStates.Clear();
            foreach (ComponentTreeHeirarchyAbilityState orphan in oldChilders)
            {
                orphan.SetParent(null);
            }

            foreach (var child in children)
            {
                child.SetParent(this);
            }


        }

        /*
         /// <summary>
         ///some ideas, implement if actualy needed
        /// </summary>
        public BaseDataBlob ThisRelatedDatablob;

        public InstancesDB ThisEntitesInstancesDB;

        (call this from Set Parent, virtual would be empty, inherited classes would have something below eg)
        protected virtual void FilterParents(ComponentTreeHeirarchyAbilityState parentToSet)
        {
            Type T = typeof(FireControlAbilityState)
            if(parentToSet is T)
            {
                if (ParentState != null)
                {
                    ParentState.ChildrenStates.Remove(this);
                }

                ParentState = newParent;
                if(newParent != null)
                    ParentState.ChildrenStates.Add(this);
            }
            else
                throw exception? fail silently? log?
        }

        protected virtual void FilterChildren(ComponentTreeHeirarchyAbilityState childToAdd)
        {
            if(childToAdd is T)
                ChildrenState.Add(childToAdd);
            else
                throw exception? fail silently? log?
        }


                /// <summary>
        /// If parent is null, will return an empty list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> GetSiblingsOfType<T>()
            where T : ComponentTreeHeirarchyAbilityState
        {
            if(ParentState == null)
                return new List<T>();
            return ParentState.GetChildrenOfType<T>();
        }


        public List<T> GetChildrenOfType<T>() where T: ComponentTreeHeirarchyAbilityState
        {
            List<T> children = new List<T>();
            foreach (var child in ChildrenStates)
            {
                if(child is T)
                    children.Add((T)child);
            }
            return children;
        }

        public ComponentInstance[] GetChildrenInstancesOfType<T>() where T: ComponentTreeHeirarchyAbilityState
        {
            var childrenStates = GetChildrenOfType<T>();
            ComponentInstance[] instances = new ComponentInstance[childrenStates.Count];
            for (int i = 0; i < childrenStates.Count; i++)
            {
                instances[i] = childrenStates[i].ComponentInstance;
            }
            return instances;
        }

        */

        public ComponentTreeHeirarchyAbilityState GetRoot()
        {
            if (ParentState != null)
                return ParentState.GetRoot();
            else
                return this;
        }



        public ComponentInstance GetRootInstance()
        {
            return GetRoot().ComponentInstance;
        }

        public List<ComponentTreeHeirarchyAbilityState> GetSiblings()
        {
            return ParentState.ChildrenStates;
        }

        public ComponentInstance[] GetChildrenInstances()
        {
            ComponentInstance[] instances = new ComponentInstance[ChildrenStates.Count];
            for (int i = 0; i < ChildrenStates.Count; i++)
            {
                instances[i] = ChildrenStates[i].ComponentInstance;
            }
            return instances;
        }


        public string[] GetChildrenIDs()
        {
            var ids = new string[ChildrenStates.Count];
            for (int i = 0; i < ChildrenStates.Count; i++)
            {
                ids[i] = ChildrenStates[i].ID;
            }

            return ids;
        }


    }
}
