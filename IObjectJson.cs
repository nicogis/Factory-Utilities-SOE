//-----------------------------------------------------------------------
// <copyright file="IObjectJson.cs" company="Studio A&T s.r.l.">
//     Copyright (c) Studio A&T s.r.l. All rights reserved.
// </copyright>
// <author>nicogis</author>
namespace Studioat.ArcGis.Soe.Rest.FactoryUtilities
{
    using System.Diagnostics.CodeAnalysis;
    using ESRI.ArcGIS.SOESupport;

    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "-")]

    /// <summary>
    /// interface for conversion in JsonObjects
    /// </summary>
    internal interface IObjectJson
    {
        /// <summary>
        /// conversion to a JsonObject
        /// </summary>
        /// <returns>return a JsonObject</returns>
        JsonObject ToJsonObject();
    }
}