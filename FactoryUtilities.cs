//-----------------------------------------------------------------------
// <copyright file="FactoryUtilities.cs" company="Studio A&T s.r.l.">
//     Copyright (c) Studio A&T s.r.l. All rights reserved.
// </copyright>
// <author>nicogis</author>
//-----------------------------------------------------------------------
namespace Studioat.ArcGis.Soe.Rest.FactoryUtilities
{
    using ESRI.ArcGIS.Carto;
    using ESRI.ArcGIS.esriSystem;
    using ESRI.ArcGIS.Geodatabase;
    using ESRI.ArcGIS.Geometry;
    using ESRI.ArcGIS.Server;
    using ESRI.ArcGIS.SOESupport;
    using SharpKml.Base;
    using SharpKml.Dom;
    using SharpKml.Engine;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Runtime.InteropServices;

    /// <summary>
    /// class Utilities
    /// </summary>
    [ComVisible(true)]
    [Guid("e8230fdc-97ea-4737-bdb3-382066b012a9")]
    [ClassInterface(ClassInterfaceType.None)]
    [ServerObjectExtension("MapServer",
        AllCapabilities = "",
        DefaultCapabilities = "",
        Description = "Factory Utilities",
        DisplayName = "Factory Utilities",
        Properties = "workspaceId=;connectionString=;rootWebAdaptor=",
        HasManagerPropertiesConfigurationPane = false,
        SupportsREST = true,
        SupportsSOAP = false)]
    public class FactoryUtilities : IServerObjectExtension, IObjectConstruct, IRESTRequestHandler
    {
        /// <summary>
        /// name of soe
        /// </summary>
        private string soeName;

        /// <summary>
        /// properties of soe
        /// </summary>
        private IPropertySet configProps;

        /// <summary>
        /// object serverObject
        /// </summary>
        private IServerObjectHelper serverObjectHelper;

        /// <summary>
        /// log arcgis server
        /// </summary>
        private ServerLogger logger;

        /// <summary>
        /// object rest request Handler
        /// </summary>
        private IRESTRequestHandler reqHandler;

        /// <summary>
        /// virtual folder of service output 
        /// </summary>
        private string pathOutputVirtualAGS;

        /// <summary>
        /// physical folder of service output 
        /// </summary>      
        private string pathOutputAGS;

        /// <summary>
        /// workspace Id per dynamic layer
        /// </summary>
        private string workspaceId = null;

        /// <summary>
        /// oggetto workspace per dynamic layer
        /// </summary>
        private IWorkspace workspace = null;

        /// <summary>
        /// message code della soe
        /// </summary>
        private int? codiceLogArcGISServerSOE = 8508;

        /// <summary>
        /// Initializes a new instance of the <see cref="FactoryUtilities"/> class
        /// </summary>
        public FactoryUtilities()
        {
            this.soeName = this.GetType().Name;
            this.logger = new ServerLogger();
            this.reqHandler = new SoeRestImpl(this.soeName, this.CreateRestSchema()) as IRESTRequestHandler;
        }

        #region IServerObjectExtension Members

        /// <summary>
        /// is called once, when the instance of the SOE is created.
        /// </summary>
        /// <param name="pSOH">object server Object</param>
        public void Init(IServerObjectHelper pSOH)
        {
            this.serverObjectHelper = pSOH;
        }

        /// <summary>
        /// shutdown() is called once when the Server Object's context is being shut down and is about to go away.
        /// </summary>
        public void Shutdown()
        {
        }

        #endregion

        #region IObjectConstruct Members

        /// <summary>
        /// construct() is called only once, when the SOE is created, after IServerObjectExtension.init() is called. This
        /// method hands back the configuration properties for the SOE as a property set. You should include any expensive
        /// initialization logic for your SOE within your implementation of construct().
        /// </summary>
        /// <param name="props">object propertySet</param>
        public void Construct(IPropertySet props)
        {
            AutoTimer timer = new AutoTimer();
            this.LogInfoSimple(this.soeName + ": il costruttore è stato avviato.", MethodBase.GetCurrentMethod().Name);
            
            try
            {
                this.configProps = props;
                if (this.configProps.GetProperty("workspaceId") is string)
                {
                    this.workspaceId = this.configProps.GetProperty("workspaceId") as string;
                }

                if (this.configProps.GetProperty("connectionString") is string)
                {
                    this.workspace = Helper.OpenFileGdbWorkspace(this.configProps.GetProperty("connectionString") as string);
                }

                IMapServer3 mapServer = this.serverObjectHelper.ServerObject as IMapServer3;
                IMapServerInit mapServerInit = mapServer as IMapServerInit;

                string hostnameWebAdaptor = string.Empty;
                if (this.configProps.GetProperty("rootWebAdaptor") is string)
                {
                    hostnameWebAdaptor = this.configProps.GetProperty("rootWebAdaptor") as string;
                }

                this.pathOutputVirtualAGS = Helper.CombineUri(hostnameWebAdaptor, mapServerInit.VirtualOutputDirectory);

                // c'è il replace perchè se il nome del servizio è in una cartella ags il percorso restituito ha '/' nel path tra folder ags e nome servizio
                this.pathOutputAGS = mapServerInit.PhysicalOutputDirectory.Replace('/', '\\');

            }
            catch (Exception ex)
            {
                this.LogError(this.soeName + ": " + ex.Message, MethodBase.GetCurrentMethod().Name);
            }

            this.LogInfoSimple(this.soeName + ": il costruttore ha concluso", MethodBase.GetCurrentMethod().Name, timer.Elapsed);
        }

        #endregion

        #region IRESTRequestHandler Members
        /// <summary>
        /// Get schema 
        /// </summary>
        /// <returns>return schema</returns>
        public string GetSchema()
        {
            return this.reqHandler.GetSchema();
        }

        /// <summary>
        /// handle rest request
        /// </summary>
        /// <param name="Capabilities">capabilities of soe</param>
        /// <param name="resourceName">name of resource</param>
        /// <param name="operationName">name of operation</param>
        /// <param name="operationInput">object operationInput</param>
        /// <param name="outputFormat">object outputFormat</param>
        /// <param name="requestProperties">object requestProperties</param>
        /// <param name="responseProperties">object responseProperties</param>
        /// <returns>return handle rest request</returns>
        public byte[] HandleRESTRequest(string Capabilities, string resourceName, string operationName, string operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            return this.reqHandler.HandleRESTRequest(Capabilities, resourceName, operationName, operationInput, outputFormat, requestProperties, out responseProperties);
        }

        #endregion

        /// <summary>
        /// create rest schema
        /// </summary>
        /// <returns>object RestResource</returns>
        private RestResource CreateRestSchema()
        {
            RestResource soeResource = new RestResource(this.soeName, false, this.RootResourceHandler);
            RestResource infoResource = new RestResource("Info", false, this.InfoResourceHandler);
            RestResource helpResource = new RestResource("Help", false, this.HelpResourceHandler);

            soeResource.resources.Add(infoResource);
            soeResource.resources.Add(helpResource);

            RestOperation extractDataOperation = new RestOperation("ExtractData",
                                                      new string[] { "featureSet", "dataFormat", "outputName", "urlLayer", "geometry", "token" },
                                                      new string[] { "json" },
                                                      this.ExtractDataOperationHandler, true);

            RestOperation dynamicLayerOperation = new RestOperation("DynamicLayer",
                                                      new string[] { "featureSet" },
                                                      new string[] { "json" },
                                                      this.DynamicLayerOperationHandler, true);



            soeResource.operations.Add(extractDataOperation);
            soeResource.operations.Add(dynamicLayerOperation);


            return soeResource;
        }

        /// <summary>
        /// Root Resource Handler
        /// </summary>
        /// <param name="boundVariables">object boundVariables</param>
        /// <param name="outputFormat">object outputFormat</param>
        /// <param name="requestProperties">object requestProperties</param>
        /// <param name="responseProperties">object responseProperties</param>
        /// <returns>object Root Resource Handler</returns>
        private byte[] RootResourceHandler(NameValueCollection boundVariables, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = null;

            JsonObject result = new JsonObject();
            AddInPackageAttribute addInPackage = (AddInPackageAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AddInPackageAttribute), false)[0];
            result.AddString("Description", addInPackage.Description);

            return result.JsonByte();
        }

        /// <summary>
        /// Returns JSON representation of Info resource. This resource is not a collection.
        /// </summary>
        /// <param name="boundVariables">object boundVariables</param>
        /// <param name="outputFormat">object outputFormat</param>
        /// <param name="requestProperties">object requestProperties</param>
        /// <param name="responseProperties">object responseProperties</param>
        /// <returns>String JSON representation of Info resource.</returns>
        private byte[] InfoResourceHandler(NameValueCollection boundVariables, string outputFormat, string requestProperties, out string responseProperties)
        {

            responseProperties = "{\"Content-Type\" : \"application/json\"}";
            JsonObject result = new JsonObject();

            AddInPackageAttribute addInPackage = (AddInPackageAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AddInPackageAttribute), false)[0];
            result.AddString("agsVersion", addInPackage.TargetVersion);
            result.AddString("soeVersion", addInPackage.Version);
            result.AddString("author", addInPackage.Author);
            result.AddString("company", addInPackage.Company);
            return result.JsonByte();

        }

        /// <summary>
        /// Returns JSON representation of Help resource. This resource is not a collection.
        /// </summary>
        /// <param name="boundVariables">list of variables bound</param>
        /// <param name="outputFormat">format of output</param>
        /// <param name="requestProperties">list of request properties</param>
        /// <param name="responseProperties">list of response properties </param>
        /// <returns>String JSON representation of Help resource.</returns>
        private byte[] HelpResourceHandler(NameValueCollection boundVariables, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";

            JsonObject result = new JsonObject();

            JsonObject extractDataInputs = new JsonObject();
            extractDataInputs.AddString("featureSet", "(string) featureSet Esri Json (obbligatorio solo per GDB/FeatureClass, Shapefile, CSV, KML)");
            extractDataInputs.AddString("dataFormat", $"(string) valori consentiti: {string.Join(",", Enum.GetNames(typeof(DataFormat)))}");
            extractDataInputs.AddString("outputName", "(string) nome del GDB/FeatureClass, Shapefile, CSV, KML o KMZ. Può contenere solo lettere o numeri. Il primo carattere non può essere un numero (nel caso viene messo il prefisso 'f') e, per il formato shapefile, non è possibile indicare più di 8 caratteri");
            extractDataInputs.AddString("urlLayer", "(string) url del layer del servizio (obbligatorio solo per kmz)");
            extractDataInputs.AddString("geometry", "(geometry) polygon Esri Json (obbligatorio solo per kmz)");
            extractDataInputs.AddString("token", "(string) opzionale token se il servizio dell'urlLayer è protetto (solo per kmz)");

            JsonObject extractDataOutputs = new JsonObject();
            extractDataOutputs.AddString("url", "(string) url del file di output in formato compresso (.zip)");
            extractDataOutputs.AddString("extraInfo", "(object) facoltativo. Per la conversione dello shapefile saranno presenti o meno due array di stringhe ('errorField' e 'invalidObjectID') se sono presenti errori di creazione campi o di creazione record");
            extractDataOutputs.AddString("hasError", "(boolean). Indica se l'operazione ha un errore");
            extractDataOutputs.AddString("errorDescription", "(string). Presente se hasError è true");

            JsonObject extractDataParams = new JsonObject();
            extractDataParams.AddString("Info", "Converte un featureSet (formato Json Esri) in file geodatabase, shapefile, csv o kmz. Il csv è consentito solo per temi di tipo point. Il kmz sarà proiettato in WGS84 ma senza trasformazione di datum");
            extractDataParams.AddJsonObject("Inputs", extractDataInputs);
            extractDataParams.AddJsonObject("Outputs", extractDataOutputs);

            JsonObject dynamicLayerInputs = new JsonObject();
            dynamicLayerInputs.AddString("featureSet", "(string) featureSet Esri Json");

            JsonObject dynamicLayerOutputs = new JsonObject();
            dynamicLayerOutputs.AddString("name", "(string) nome della feature class");
            dynamicLayerOutputs.AddString("workspaceId", "(string) workspaceId");
            dynamicLayerOutputs.AddString("hasError", "(boolean). Indica se l'operazione ha un errore");
            dynamicLayerOutputs.AddString("errorDescription", "(string). Presente se hasError è true");

            JsonObject dynamicLayerParams = new JsonObject();
            dynamicLayerParams.AddString("Info", "Dynamic layer");
            dynamicLayerParams.AddJsonObject("Inputs", dynamicLayerInputs);
            dynamicLayerParams.AddJsonObject("Outputs", dynamicLayerOutputs);

            JsonObject soeOperations = new JsonObject();
            soeOperations.AddJsonObject("ExtractData", extractDataParams);
            soeOperations.AddJsonObject("DynamicLayer", dynamicLayerParams);

            result.AddJsonObject("Operations", soeOperations);

            return result.JsonByte();
        }

        /// <summary>
        /// Operation Extract Data
        /// </summary>
        /// <param name="boundVariables">bound Variables</param>
        /// <param name="operationInput">operation Input</param>
        /// <param name="outputFormat">output Format</param>
        /// <param name="requestProperties">request Properties</param>
        /// <param name="responseProperties">response Properties</param>
        /// <returns>Extract Data</returns>
        private byte[] ExtractDataOperationHandler(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";

            try
            {
                // input parameters

                // input data format
                string dataFormatValue;
                bool found = operationInput.TryGetString("dataFormat", out dataFormatValue);
                if (!found || string.IsNullOrEmpty(dataFormatValue))
                {
                    throw new ArgumentNullException(nameof(dataFormatValue));
                }

                DataFormat dataFormat;

                if (!Enum.TryParse<DataFormat>(dataFormatValue, out dataFormat))
                {
                    throw new ArgumentNullException(nameof(dataFormatValue));
                }

                // il parametro urlLayer e geometry sono obbligatorio per il KMZ
                string urlLayerValue = null;
                JsonObject geometryValue = null;
                string tokenValue = null;

                // input featureset
                JsonObject featureSetValue = null;

                if (dataFormat == DataFormat.KMZ)
                {
                    found = operationInput.TryGetString("urlLayer", out urlLayerValue);
                    if (!found || string.IsNullOrEmpty(urlLayerValue))
                    {
                        throw new ArgumentNullException(nameof(urlLayerValue));
                    }

                    urlLayerValue = urlLayerValue.TrimEnd('/');

                    found = operationInput.TryGetString("token", out tokenValue);


                    found = operationInput.TryGetJsonObject("geometry", out geometryValue);
                    if (!found)
                    {
                        throw new ArgumentNullException(nameof(geometryValue));
                    }
                }
                else
                {
                    found = operationInput.TryGetJsonObject("featureSet", out featureSetValue);
                    if (!found)
                    {
                        throw new ArgumentNullException(nameof(featureSetValue));
                    }
                }

                // input outputName
                string outputNameValue;
                found = operationInput.TryGetString("outputName", out outputNameValue);
                if (!found || string.IsNullOrEmpty(outputNameValue))
                {
                    throw new ArgumentNullException(nameof(outputNameValue));
                }

                // lascio solo caratteri e numeri
                outputNameValue = new String(outputNameValue.Where(Char.IsLetterOrDigit).ToArray());

                if (string.IsNullOrEmpty(outputNameValue))
                {
                    throw new ArgumentNullException(nameof(outputNameValue));
                }

                // se il nome inizia con un numero metto davanti una lettera 
                if (Char.IsDigit(outputNameValue.FirstOrDefault()))
                {
                    outputNameValue = $"f{outputNameValue}";
                }

                // se il formato è shapefile il nome del file lo tronco a max 8 caratteri
                if ((dataFormat == DataFormat.SHAPEFILE) && (outputNameValue.Length > 8))
                {
                    outputNameValue = outputNameValue.Substring(0, 8);
                }

                // creazione cartella di output
                string folderNameOutput = Guid.NewGuid().ToString();
                string pathOutput = System.IO.Path.Combine(this.pathOutputAGS, folderNameOutput);

                if (Directory.Exists(pathOutput))
                {
                    // non dovrebbe mai verificarsi
                    throw new FactoryUtilitiesException($"Cartella {pathOutput} già esistente");
                }
                else
                {
                    Directory.CreateDirectory(pathOutput);
                }

                // cartella kmz
                string kmzFolder = null;

                // cartella shapefile
                string shapefileFolder = null;

                // cartella csv
                string csvFolder = null;

                // cartella kml
                string kmlFolder = null;

                // file geodatabase
                string fGDBFolder = null;

                JsonObject message = null;

                if (dataFormat == DataFormat.KMZ)
                {
                    kmzFolder = System.IO.Path.Combine(pathOutput, "kmz");
                    Directory.CreateDirectory(kmzFolder);
                    this.ConvertFeatureClassPointToKmz(urlLayerValue, geometryValue, tokenValue, outputNameValue, kmzFolder); 
                }
                else
                {

                    List<string> errorFieldShapeFile = null;
                    List<string> invalidObjectIDShapeFile = null;

                    IJSONReader jsonReader = new JSONReaderClass();
                    jsonReader.ReadFromString(featureSetValue.ToJson());

                    IJSONConverterGdb JSONConverterGdb = new JSONConverterGdbClass();
                    IPropertySet originalToNewFieldMap;
                    IRecordSet recorset;
                    JSONConverterGdb.ReadRecordSet(jsonReader, null, null, out recorset, out originalToNewFieldMap);

                    IRecordSet2 recordSet2 = recorset as IRecordSet2;

                    ITable t = null;
                    IWorkspace workspaceGDB = null;
                    try
                    {
                        // nome del file geodatabase con estensione
                        string nameGDB = System.IO.Path.ChangeExtension(outputNameValue, Enum.GetName(typeof(FileExtension), FileExtension.gdb));

                        // creazione del file geodatabase
                        workspaceGDB = Helper.CreateFileGdbWorkspace(pathOutput, nameGDB);
                        t = recordSet2.SaveAsTable(workspaceGDB, outputNameValue);

                        if (dataFormat == DataFormat.SHAPEFILE)
                        {
                            shapefileFolder = System.IO.Path.Combine(pathOutput, Enum.GetName(typeof(DataFormat), DataFormat.SHAPEFILE).ToLowerInvariant());
                            errorFieldShapeFile = new List<string>();
                            invalidObjectIDShapeFile = new List<string>();
                            Directory.CreateDirectory(shapefileFolder);
                            this.ConvertFeatureClassToShapefile(workspaceGDB, outputNameValue, shapefileFolder, ref errorFieldShapeFile, ref invalidObjectIDShapeFile);

                            if ((errorFieldShapeFile.Count > 0) || (invalidObjectIDShapeFile.Count > 0))
                            {
                                message = new JsonObject();
                                message.AddArray("errorField", errorFieldShapeFile.ToArray());
                                message.AddArray("invalidObjectID", invalidObjectIDShapeFile.ToArray());
                            }
                        }
                        else if (dataFormat == DataFormat.CSV)
                        {
                            csvFolder = System.IO.Path.Combine(pathOutput, Enum.GetName(typeof(DataFormat), DataFormat.CSV).ToLowerInvariant());
                            Directory.CreateDirectory(csvFolder);
                            this.ConvertFeatureClassPointToCsv(workspaceGDB, outputNameValue, csvFolder);
                        }
                        else if (dataFormat == DataFormat.KML)
                        {
                            // funzione per creare il file kml con la libreria SharpKml
                            // file è senza render
                            kmlFolder = System.IO.Path.Combine(pathOutput, Enum.GetName(typeof(DataFormat), DataFormat.KML).ToLowerInvariant());
                            Directory.CreateDirectory(kmlFolder);
                            this.ConvertFeatureClassPointToKml(workspaceGDB, outputNameValue, kmlFolder); 
                        }
                        else if (dataFormat == DataFormat.FILEGEODATABASE)
                        {
                            fGDBFolder = System.IO.Path.Combine(pathOutput, nameGDB);
                        }
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                        Marshal.FinalReleaseComObject(t);
                        Marshal.FinalReleaseComObject(workspaceGDB);
                    }
                }

                // nome del file zip con estensione
                string nameZip = System.IO.Path.ChangeExtension(outputNameValue, Enum.GetName(typeof(FileExtension), FileExtension.zip));

                // percorso e nome file zip con estensione
                string pathFileOutputZip = System.IO.Path.Combine(pathOutput, nameZip);

                if (dataFormat == DataFormat.FILEGEODATABASE)
                {
                    ZipFile.CreateFromDirectory(fGDBFolder, pathFileOutputZip, CompressionLevel.Fastest, true);
                }
                else if (dataFormat == DataFormat.SHAPEFILE)
                {
                    ZipFile.CreateFromDirectory(shapefileFolder, pathFileOutputZip, CompressionLevel.Fastest, true);
                }
                else if (dataFormat == DataFormat.CSV)
                {
                    ZipFile.CreateFromDirectory(csvFolder, pathFileOutputZip, CompressionLevel.Fastest, true);
                }
                else if (dataFormat == DataFormat.KML)
                {
                    ZipFile.CreateFromDirectory(kmlFolder, pathFileOutputZip, CompressionLevel.Fastest, true);
                }
                else if (dataFormat == DataFormat.KMZ)
                {
                    ZipFile.CreateFromDirectory(kmzFolder, pathFileOutputZip, CompressionLevel.Fastest, true);
                }

                JsonObject result = new JsonObject();
                result.AddString("url", Helper.CombineUri(this.pathOutputVirtualAGS, Helper.CombineUri(folderNameOutput, nameZip)));
                result.AddBoolean("hasError", false);

                if (message != null)
                {
                    result.AddJsonObject("extraInfo", message);
                }

                return result.JsonByte();
            }
            catch (Exception ex)
            {
                ObjectError o = new ObjectError(ex.Message);
                return o.ToJsonObject().JsonByte();
            }

        }

        /// <summary>
        /// Operation Dynamic Layer
        /// </summary>
        /// <param name="boundVariables">bound Variables</param>
        /// <param name="operationInput">operation Input</param>
        /// <param name="outputFormat">output Format</param>
        /// <param name="requestProperties">request Properties</param>
        /// <param name="responseProperties">response Properties</param>
        /// <returns>Dynamic Layer</returns>
        private byte[] DynamicLayerOperationHandler(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";

            try
            {
                JsonObject featureSet;
                bool found = operationInput.TryGetJsonObject("featureSet", out featureSet);
                if (!found)
                {
                    throw new ArgumentNullException(nameof(featureSet));
                }

                if (this.workspace == null)
                {
                    throw new FactoryUtilitiesException("Workspace non impostato!");
                }

                if (string.IsNullOrWhiteSpace(this.workspaceId))
                {
                    throw new FactoryUtilitiesException("WorkspaceId non impostato!");
                }

                IJSONReader jsonReader = new JSONReaderClass();
                jsonReader.ReadFromString(featureSet.ToJson());

                IJSONConverterGdb JSONConverterGdb = new JSONConverterGdbClass();
                IPropertySet originalToNewFieldMap;
                IRecordSet recorset;
                JSONConverterGdb.ReadRecordSet(jsonReader, null, null, out recorset, out originalToNewFieldMap);

                IRecordSet2 recordSet2 = recorset as IRecordSet2;

                string featureClassName = string.Format("D{0}{1}", DateTime.Now.ToString("yyyyMMdd"), Guid.NewGuid().ToString()).Replace("-", "");

                recordSet2.SaveAsTable(this.workspace, featureClassName);

                JsonObject result = new JsonObject();
                result.AddString("name", featureClassName);
                result.AddString("workspaceId", this.workspaceId);
                result.AddBoolean("hasError", false);
                return result.JsonByte();
            }
            catch (Exception ex)
            {
                ObjectError o = new ObjectError(ex.Message);
                return o.ToJsonObject().JsonByte();
            }
        }

        #region Extract Data
        /// <summary>
        /// convert featureclass in shapefile
        /// </summary>
        /// <param name="sourceWorkspace">oggetto workspace</param>
        /// <param name="outputName">nome feature class e nome shapefile</param>
        /// <param name="targetWorkspacePath">cartella shapefile</param>
        /// <param name="errorField">lista degli eventuali errori nella creazione dei campi</param>
        /// <param name="invalidObject">lista degli eventuale errori di creazione record</param>
        private void ConvertFeatureClassToShapefile(IWorkspace sourceWorkspace, string outputName, string targetWorkspacePath, ref List<string> errorField, ref List<string> invalidObject)
        {
            IWorkspace targetWorkspace = null;
            try
            {
                Type factoryType = Type.GetTypeFromProgID("esriDataSourcesFile.ShapefileWorkspaceFactory");
                IWorkspaceFactory targetWorkspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
                targetWorkspace = targetWorkspaceFactory.OpenFromFile(targetWorkspacePath, 0);

                // Cast the workspaces to the IDataset interface and get name objects.
                IDataset sourceWorkspaceDataset = (IDataset)sourceWorkspace;
                IDataset targetWorkspaceDataset = (IDataset)targetWorkspace;
                IName sourceWorkspaceDatasetName = sourceWorkspaceDataset.FullName;
                IName targetWorkspaceDatasetName = targetWorkspaceDataset.FullName;
                IWorkspaceName sourceWorkspaceName = (IWorkspaceName)sourceWorkspaceDatasetName;
                IWorkspaceName targetWorkspaceName = (IWorkspaceName)targetWorkspaceDatasetName;

                // Create a name object for the shapefile and cast it to the IDatasetName interface.
                IFeatureClassName sourceFeatureClassName = new FeatureClassNameClass();
                IDatasetName sourceDatasetName = (IDatasetName)sourceFeatureClassName;
                sourceDatasetName.Name = outputName;
                sourceDatasetName.WorkspaceName = sourceWorkspaceName;

                // Create a name object for the FGDB feature class and cast it to the IDatasetName interface.
                IFeatureClassName targetFeatureClassName = new FeatureClassNameClass();
                IDatasetName targetDatasetName = (IDatasetName)targetFeatureClassName;
                targetDatasetName.Name = outputName;
                targetDatasetName.WorkspaceName = targetWorkspaceName;

                // Open source feature class to get field definitions.
                IName sourceName = (IName)sourceFeatureClassName;
                IFeatureClass sourceFeatureClass = (IFeatureClass)sourceName.Open();

                // Create the objects and references necessary for field validation.
                IFieldChecker fieldChecker = new FieldCheckerClass();
                IFields sourceFields = sourceFeatureClass.Fields;
                IFields targetFields = null;
                IEnumFieldError enumFieldError = null;

                // Set the required properties for the IFieldChecker interface.
                fieldChecker.InputWorkspace = sourceWorkspace;
                fieldChecker.ValidateWorkspace = targetWorkspace;

                // Validate the fields and check for errors.
                fieldChecker.Validate(sourceFields, out enumFieldError, out targetFields);
                if (enumFieldError != null)
                {
                    IFieldError fieldError = null;
                    enumFieldError.Reset();
                    while ((fieldError = enumFieldError.Next()) != null)
                    {
                        errorField.Add($"Errore: {Enum.GetName(typeof(esriFieldNameErrorType), fieldError.FieldError)} - Campo: {targetFields.get_Field(fieldError.FieldIndex).Name}");
                    }
                }

                // Find the shape field.
                string shapeFieldName = sourceFeatureClass.ShapeFieldName;
                int shapeFieldIndex = sourceFeatureClass.FindField(shapeFieldName);
                IField shapeField = sourceFields.get_Field(shapeFieldIndex);

                // Get the geometry definition from the shape field and clone it.
                IGeometryDef geometryDef = shapeField.GeometryDef;
                IClone geometryDefClone = (IClone)geometryDef;
                IClone targetGeometryDefClone = geometryDefClone.Clone();
                IGeometryDef targetGeometryDef = (IGeometryDef)targetGeometryDefClone;

                // Create the converter and run the conversion.
                IFeatureDataConverter featureDataConverter = new FeatureDataConverterClass();
                IEnumInvalidObject enumInvalidObject =
                  featureDataConverter.ConvertFeatureClass(sourceFeatureClassName,
                  null, null, targetFeatureClassName, targetGeometryDef, targetFields,
                  string.Empty, 1000, 0);

                // Check for errors.
                IInvalidObjectInfo invalidObjectInfo = null;
                enumInvalidObject.Reset();
                while ((invalidObjectInfo = enumInvalidObject.Next()) != null)
                {
                    invalidObject.Add($"{invalidObjectInfo.InvalidObjectID}");
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (targetWorkspace != null)
                {
                    Marshal.FinalReleaseComObject(targetWorkspace);
                }
            }
        }

        /// <summary>
        /// converte la feature class in csv. Se non sono presenti le coordinate il dato non viene inserito
        /// </summary>
        /// <param name="sourceWorkspace">workspace con la feature class</param>
        /// <param name="outputName">nome della feature class e del csv di output</param>
        /// <param name="targetPath">percorso dove salvare il file di output</param>
        /// <param name="delimitator">delimitatore utilizzato nel csv</param>
        private void ConvertFeatureClassPointToCsv(IWorkspace sourceWorkspace, string outputName, string targetPath, string delimitator = ";")
        {
            IFeatureCursor featureCursor = null;
            try
            {
                IFeatureWorkspace featureWorkspace = sourceWorkspace as IFeatureWorkspace;
                IFeatureClass featureClass = featureWorkspace.OpenFeatureClass(outputName);
                if (featureClass.ShapeType != esriGeometryType.esriGeometryPoint)
                {
                    throw new Exception($"Per esportare in {Enum.GetName(typeof(FileExtension), FileExtension.csv)} occorre una feature class di tipo {Enum.GetName(typeof(esriGeometryType), esriGeometryType.esriGeometryPoint)}!");
                }

                featureCursor = featureClass.Search(null, true);

                CsvExport csvExport = new CsvExport();
                csvExport.Delimiter = delimitator;

                Dictionary<int, string> fields = new Dictionary<int, string>();
                IField field = null;
                for (int i = 0; i < featureCursor.Fields.FieldCount; i++)
                {
                    field = featureCursor.Fields.Field[i];
                    if ((field.Type == esriFieldType.esriFieldTypeBlob) || (field.Type == esriFieldType.esriFieldTypeGeometry) ||
                        (field.Type == esriFieldType.esriFieldTypeGlobalID) || (field.Type == esriFieldType.esriFieldTypeGUID) ||
                        (field.Type == esriFieldType.esriFieldTypeRaster) || (field.Type == esriFieldType.esriFieldTypeXML))
                    {
                        continue;
                    }

                    fields.Add(i, field.Name);
                }

                IFeature feature = null;
                while ((feature = featureCursor.NextFeature()) != null)
                {
                    if ((feature.ShapeCopy == null) || (feature.ShapeCopy.IsEmpty))
                    {
                        continue;
                    }

                    IPoint p = feature.ShapeCopy as IPoint;
                    csvExport.AddRow();
                    csvExport["X"] = p.X;
                    csvExport["Y"] = p.Y;

                    foreach (int i in fields.Keys)
                    {
                        csvExport[fields[i]] = feature.get_Value(i);
                    }
                }

                csvExport.ExportToFile(System.IO.Path.Combine(targetPath, System.IO.Path.ChangeExtension(outputName, Enum.GetName(typeof(FileExtension), FileExtension.csv))));
            }
            catch
            {
                throw;
            }
            finally
            {
                if (featureCursor != null)
                {
                    Marshal.FinalReleaseComObject(featureCursor);
                }
            }
        }

        /// <summary>
        /// converte la feature class in kml. Se non sono presenti le coordinate il dato non viene inserito
        /// </summary>
        /// <param name="sourceWorkspace">workspace con la feature class</param>
        /// <param name="outputName">nome della feature class e del csv di output</param>
        /// <param name="targetPath">percorso dove salvare il file di output</param>
        /// <param name="delimitator">delimitatore utilizzato nel csv</param>
        private void ConvertFeatureClassPointToKml(IWorkspace sourceWorkspace, string outputName, string targetPath)
        {
            IFeatureCursor featureCursor = null;
            try
            {
                IFeatureWorkspace featureWorkspace = sourceWorkspace as IFeatureWorkspace;
                IFeatureClass featureClass = featureWorkspace.OpenFeatureClass(outputName);
                if (featureClass.ShapeType != esriGeometryType.esriGeometryPoint)
                {
                    throw new Exception($"Per esportare in {Enum.GetName(typeof(FileExtension), FileExtension.kmz)} occorre una feature class di tipo {Enum.GetName(typeof(esriGeometryType), esriGeometryType.esriGeometryPoint)}!");
                }

                featureCursor = featureClass.Search(null, true);

                var folder = new Folder();
                folder.Id = outputName;
                folder.Name = outputName;

                Dictionary<int, string> fields = new Dictionary<int, string>();
                IField field = null;
                for (int i = 0; i < featureCursor.Fields.FieldCount; i++)
                {
                    field = featureCursor.Fields.Field[i];
                    if ((field.Type == esriFieldType.esriFieldTypeBlob) || (field.Type == esriFieldType.esriFieldTypeGeometry) ||
                        (field.Type == esriFieldType.esriFieldTypeGlobalID) || (field.Type == esriFieldType.esriFieldTypeGUID) ||
                        (field.Type == esriFieldType.esriFieldTypeRaster) || (field.Type == esriFieldType.esriFieldTypeXML))
                    {
                        continue;
                    }

                    fields.Add(i, field.Name);
                }

                IFeature feature = null;
                Placemark placemark = null;
                Vector vector = null;
                ExtendedData extendedData = null;
                while ((feature = featureCursor.NextFeature()) != null)
                {
                    if ((feature.ShapeCopy == null) || (feature.ShapeCopy.IsEmpty))
                    {
                        continue;
                    }

                    IPoint p = feature.ShapeCopy as IPoint;
                    if (p.SpatialReference.FactoryCode != Helper.SpatialReferenceWGS84.FactoryCode)
                    {
                        p.Project(Helper.SpatialReferenceWGS84);
                    }

                    vector = new Vector(p.Y, p.X);


                    placemark = new Placemark();
                    placemark.Id = feature.OID.ToString();
                    placemark.Name = feature.OID.ToString();

                    extendedData = new ExtendedData();
                    foreach (int i in fields.Keys)
                    {
                        Data data = new Data();
                        data.DisplayName = fields[i];
                        data.Name = fields[i];
                        data.Value = feature.get_Value(i).ToString();
                        extendedData.AddData(data);
                    }

                    placemark.ExtendedData = extendedData;
                    placemark.Geometry = new SharpKml.Dom.Point { Coordinate = vector };

                    folder.AddFeature(placemark);
                }

                Kml kml = new Kml();
                kml.AddNamespacePrefix(KmlNamespaces.GX22Prefix, KmlNamespaces.GX22Namespace);
                kml.Feature = folder;
                KmlFile kmlfile = KmlFile.Create(kml, false);
                using (var stream = File.OpenWrite(System.IO.Path.Combine(targetPath, System.IO.Path.ChangeExtension(outputName, Enum.GetName(typeof(FileExtension), FileExtension.kmz)))))
                {
                    kmlfile.Save(stream);
                }

            }
            catch
            {
                throw;
            }
            finally
            {
                if (featureCursor != null)
                {
                    Marshal.FinalReleaseComObject(featureCursor);
                }
            }
        }

        private void ConvertFeatureClassPointToKmz(string urlLayer, JsonObject geometryValue, string token, string outputName, string targetPath)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    NameValueCollection nc = new NameValueCollection();
                    nc.Add("geometry", geometryValue.ToJson());

                    IGeometry geometry = geometryValue.ConvertAnyJsonGeom();
                    
                    nc.Add("geometryType", Enum.GetName(typeof(esriGeometryType), geometry.GeometryType));
                    nc.Add("outFields", '*'.ToString());
                    nc.Add("spatialRel", Enum.GetName(typeof(esriSpatialRelEnum), esriSpatialRelEnum.esriSpatialRelIntersects)); // cmq valore di default
                    nc.Add("returnGeometry", bool.TrueString.ToLowerInvariant()); // cmq valore di default
                    nc.Add("f", Enum.GetName(typeof(FileExtension), FileExtension.kmz));

                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        nc.Add("token", token);
                    }


                    byte[] response = client.UploadValues($"{urlLayer}/query", nc);

                    File.WriteAllBytes(System.IO.Path.Combine(targetPath, System.IO.Path.ChangeExtension(outputName, Enum.GetName(typeof(FileExtension), FileExtension.kmz))), response);
                }
            }
            catch
            {
                throw;
            }
        }

        #endregion

        #region Log
        private void Log(string message, string nameMethod, double timer, ServerLogger.msgType tipo)
        {
            if (double.IsNaN(timer))
            {
                this.logger.LogMessage(tipo, nameMethod, this.codiceLogArcGISServerSOE.Value, message);
            }
            else
            {
                this.logger.LogMessage(tipo, nameMethod, this.codiceLogArcGISServerSOE.Value, timer, message);
            }
        }

        private void LogInfoSimple(string message, string nameMethod, double timer)
        {
            this.Log(message, nameMethod, timer, ServerLogger.msgType.infoSimple);
        }

        private void LogInfoSimple(string message, string nameMethod)
        {
            this.LogInfoSimple(message, nameMethod, double.NaN);
        }

        private void LogError(string message, string nameMethod, double timer)
        {
            this.Log(message, nameMethod, timer, ServerLogger.msgType.error);
        }

        private void LogError(string message, string nameMethod)
        {
            this.LogError(message, nameMethod, double.NaN);
        }

        private void LogWarning(string message, string nameMethod, double timer)
        {
            this.Log(message, nameMethod, timer, ServerLogger.msgType.warning);
        }

        private void LogWarning(string message, string nameMethod)
        {
            this.LogWarning(message, nameMethod, double.NaN);
        }

        private void LogDebug(string message, string nameMethod, double timer)
        {
            this.Log(message, nameMethod, timer, ServerLogger.msgType.debug);
        }

        private void LogDebug(string message, string nameMethod)
        {
            this.LogDebug(message, nameMethod, double.NaN);
        }

        private void LogInfoStandard(string message, string nameMethod, double timer)
        {
            this.Log(message, nameMethod, timer, ServerLogger.msgType.infoStandard);
        }

        private void LogInfoStandard(string message, string nameMethod)
        {
            this.LogInfoStandard(message, nameMethod, double.NaN);
        }

        private void LogInfoDetailed(string message, string nameMethod, double timer)
        {
            this.Log(message, nameMethod, timer, ServerLogger.msgType.infoDetailed);
        }

        private void LogInfoDetailed(string message, string nameMethod)
        {
            this.LogInfoDetailed(message, nameMethod, double.NaN);
        }
        #endregion
    }
}
