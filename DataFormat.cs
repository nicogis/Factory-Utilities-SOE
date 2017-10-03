//-----------------------------------------------------------------------
// <copyright file="DataFormat.cs" company="Studio A&T s.r.l.">
//     Copyright (c) Studio A&T s.r.l. All rights reserved.
// </copyright>
// <author>nicogis</author>
//-----------------------------------------------------------------------
namespace Studioat.ArcGis.Soe.Rest.FactoryUtilities
{
    /// <summary>
    /// format output
    /// </summary>
    internal enum DataFormat
    {
        /// <summary>
        /// format file geodatabase
        /// </summary>
        FILEGEODATABASE,

        /// <summary>
        /// output shapefile
        /// </summary>
        SHAPEFILE,

        /// <summary>
        /// output kmz
        /// </summary>
        KMZ,

        /// <summary>
        /// output csv
        /// </summary>
        CSV,

        /// <summary>
        /// output kml
        /// </summary>
        KML
    }
}
