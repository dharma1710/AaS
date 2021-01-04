/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;

// reSharper disable UnusedType.Global
// reSharper disable ClassNeverInstantiated.Global

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// This class holds definitions, which are preliminary, experimental, partial, not stabilized.
    /// The definitions aim as the definition and handling of Events.
    /// The should end up finally in a AASiD specification.
    /// </summary>
    public class AasEvents : AasxDefinitionBase
    {
        public static AasEvents Static = new AasEvents();

        public AdminShell.ConceptDescription
            CD_UpdateValueOutwards;

        public AasEvents()
        {
            // info
            this.DomainInfo = "AAS Events";

            // definitons
            CD_UpdateValueOutwards = CreateSparseConceptDescription("en", "IRI",
                "UpdateValueOutwards",
                "https://admin-shell.io/tmp/AAS/Events/UpdateValueOutwards",
                @"TBD.");

            // reflect
            AddEntriesByReflection(this.GetType(), useAttributes: false, useFieldNames: true);
        }
    }
}
