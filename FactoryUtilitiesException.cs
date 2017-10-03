//-----------------------------------------------------------------------
// <copyright file="FactoryUtilitiesException.cs" company="Studio A&T s.r.l.">
//  Copyright (c) Studio A&T s.r.l. All rights reserved.
// </copyright>
// <author>nicogis</author>
//-----------------------------------------------------------------------
namespace Studioat.ArcGis.Soe.Rest.FactoryUtilities
{ 
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// class UtilitiesException Exception
    /// </summary>
    [Serializable]
    public class FactoryUtilitiesException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the UtilitiesException class
        /// </summary>
        public FactoryUtilitiesException() : base()
        {

        }

        /// <summary>
        /// Initializes a new instance of the UtilitiesException class
        /// </summary>
        /// <param name="message">message error</param>
        public FactoryUtilitiesException(string message)
            : base(message)
        {

        }

        /// <summary>
        /// Initializes a new instance of the UtilitiesException class
        /// </summary>
        /// <param name="message">message error</param>
        /// <param name="innerException">object Exception</param>
        public FactoryUtilitiesException(string message, Exception innerException) : base(message, innerException)
        {

        }

        /// <summary>
        /// Initializes a new instance of the UtilitiesException class
        /// </summary>
        /// <param name="info">object SerializationInfo</param>
        /// <param name="context">object StreamingContext</param>
        protected FactoryUtilitiesException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
