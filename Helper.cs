//-----------------------------------------------------------------------
// <copyright file="Helper.cs" company="Studio A&T s.r.l.">
//     Copyright (c) Studio A&T s.r.l. All rights reserved.
// </copyright>
// <author>nicogis</author>
namespace Studioat.ArcGis.Soe.Rest.FactoryUtilities
{
    using System;
    using ESRI.ArcGIS.esriSystem;
    using ESRI.ArcGIS.Geodatabase;
    using ESRI.ArcGIS.Geometry;

    /// <summary>
    /// classe di helper del progetto corrente
    /// </summary>
    internal static class Helper
    {
        /// <summary>
        /// wkid web mercator indicato da Esri prima che EPSG definisse il 3857
        /// </summary>
        internal static readonly int gcsTypesriSRProjCS_WGS1984WebMercatorMajorAuxSphereEsri = 102100;

        /// <summary>
        /// spatial reference WGS84
        /// </summary>
        internal static readonly ISpatialReference SpatialReferenceWGS84 = CreateGeographicCoordinateSystem(esriSRGeoCSType.esriSRGeoCS_WGS1984);

        /// <summary>
        /// spatial reference web mercator
        /// </summary>
        internal static readonly ISpatialReference SpatialReferenceWebMercator = Helper.CreateProjectedCoordinateSystem((int)esriSRProjCS3Type.esriSRProjCS_WGS1984WebMercatorMajorAuxSphere);

        /// <summary>
        /// verifica se lo spatial reference è web mercator
        /// </summary>
        /// <param name="spatialReference">spatial reference da controllare</param>
        /// <returns>restituisce true se lo spatial reference è web mercator</returns>
        internal static bool IsWebMercator(ISpatialReference spatialReference)
        {
            return (spatialReference.FactoryCode == gcsTypesriSRProjCS_WGS1984WebMercatorMajorAuxSphereEsri) || (spatialReference.FactoryCode == ((int)esriSRProjCS3Type.esriSRProjCS_WGS1984WebMercatorMajorAuxSphere));
        }

        /// <summary>
        /// crea un file geodatabase
        /// </summary>
        /// <param name="path">percorso e nome del geodatabase</param>
        /// <param name="nameWithExtension">nome del file con estensione</param>
        /// <returns>IWorkspace del file geodatabase</returns>
        internal static IWorkspace CreateFileGdbWorkspace(string path, string nameWithExtension)
        {
            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            IWorkspaceName workspaceName = workspaceFactory.Create(path, nameWithExtension, null, 0);

            IName name = (IName)workspaceName;
            IWorkspace workspace = (IWorkspace)name.Open();
            return workspace;
        }

        /// <summary>
        /// workspace file geodatabase
        /// </summary>
        /// <param name="pathAndFile">percorso e nome cartella fgdb</param>
        /// <returns> workspace file geodatabase</returns>
        internal static IWorkspace OpenFileGdbWorkspace(string pathAndFile)
        {
            if (string.IsNullOrWhiteSpace(pathAndFile))
            {
                return null;
            }

            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            return workspaceFactory.OpenFromFile(pathAndFile, 0);
        }

        /// <summary>
        /// crea una GeographicCoordinateSystem da una esriSRGeoCSType
        /// </summary>
        /// <param name="gcs">parametro esriSRGeoCSType</param>
        /// <returns>restituisce GeographicCoordinateSystem</returns>
        internal static IGeographicCoordinateSystem CreateGeographicCoordinateSystem(esriSRGeoCSType gcs)
        {
            return CreateGeographicCoordinateSystem((int)gcs);
        }

        /// <summary>
        /// crea una GeographicCoordinateSystem da un esriSRGeoCSType (int)
        /// </summary>
        /// <param name="gcsType">parametro esriSRGeoCSType (int)</param>
        /// <returns>restituisce GeographicCoordinateSystem</returns>
        internal static IGeographicCoordinateSystem CreateGeographicCoordinateSystem(int gcsType)
        {
            ISpatialReferenceFactory spatialReferenceFactory = Helper.CreateSpatialReferenceFactory();
            return spatialReferenceFactory.CreateGeographicCoordinateSystem(gcsType);
        }

        /// <summary>
        /// crea uno SpatialReferenceFactory
        /// </summary>
        /// <returns>restituisce SpatialReferenceFactory</returns>
        internal static ISpatialReferenceFactory CreateSpatialReferenceFactory()
        {
            Type t = Type.GetTypeFromProgID("esriGeometry.SpatialReferenceEnvironment");
            object obj = Activator.CreateInstance(t);
            return obj as ISpatialReferenceFactory;
        }

        /// <summary>
        /// crea una ProjectedCoordinateSystem da un esriSRProjCSType
        /// </summary>
        /// <param name="gcs">parametro esriSRProjCSType</param>
        /// <returns>restituisce ProjectedCoordinateSystem</returns>
        internal static IProjectedCoordinateSystem CreateProjectedCoordinateSystem(esriSRProjCSType gcs)
        {
            return Helper.CreateProjectedCoordinateSystem((int)gcs);
        }

        /// <summary>
        /// crea una ProjectedCoordinateSystem da un esriSRProjCSType (int)
        /// </summary>
        /// <param name="gcsType">parametro esriSRProjCSType (int)</param>
        /// <returns>restituisce ProjectedCoordinateSystem</returns>
        internal static IProjectedCoordinateSystem CreateProjectedCoordinateSystem(int gcsType)
        {
            ISpatialReferenceFactory spatialReferenceFactory = Helper.CreateSpatialReferenceFactory();
            return spatialReferenceFactory.CreateProjectedCoordinateSystem(gcsType);
        }

        /// <summary>
        /// combina uri
        /// </summary>
        /// <param name="uri1">primo uri</param>
        /// <param name="uri2">secondo uri</param>
        /// <returns>restituisce la combinazione dei due uri</returns>
        internal static string CombineUri(string uri1, string uri2)
        {
            uri1 = uri1.TrimEnd('/');
            uri2 = uri2.TrimStart('/');
            return $"{uri1}/{uri2}";
        }
    }
}
