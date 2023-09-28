using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using NCalc;
using NCalc.Domain;
using Pulsar4X.Components;
using Pulsar4X.Engine;

namespace Pulsar4X.Interfaces
{
    public interface IComponentDesignAttribute
    {
        //void OnComponentInstantiation(Entity component);
        void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance);
        void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance);

        string AtbName();

        string AtbDescription();
    }
}