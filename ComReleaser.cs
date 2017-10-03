namespace Studioat.ArcGis.Soe.Rest.FactoryUtilities
{

        using System;
        using System.Collections;
        using System.Runtime.InteropServices;

        [Serializable]
        public class ComReleaser : IDisposable
        {
            private ArrayList array = ArrayList.Synchronized(new ArrayList());

            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                int count = this.array.Count;
                for (int i = 0; i < count; i++)
                {
                    if ((this.array[i] != null) && Marshal.IsComObject(this.array[i]))
                    {
                        while (Marshal.ReleaseComObject(this.array[i]) > 0)
                        {
                        }
                    }
                }
                if (disposing)
                {
                    this.array = null;
                }
            }

            ~ComReleaser()
            {
                this.Dispose(false);
            }

            public void ManageLifetime(object o)
            {
                this.array.Add(o);
            }

            public static void ReleaseCOMObject(object o)
            {
                if ((o != null) && Marshal.IsComObject(o))
                {
                    while (Marshal.ReleaseComObject(o) > 0)
                    {
                    }
                }
            }
        }
}
