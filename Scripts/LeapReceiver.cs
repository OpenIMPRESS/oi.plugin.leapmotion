using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Leap.Unity;
using oi.core.network;

namespace oi.plugin.leapmotion {
    /**LeapServiceProvider creates a Controller and supplies Leap Hands and images */
    public class LeapReceiver : MonoBehaviour {
        public UDPConnector udpConnector;
        Leap.Frame _CurrentFrame = null;

        // Use this for initialization
        void Start() {

        }

        // Update is called once per frame
        void Update() {
            ParseData(udpConnector);
        }


        private void ParseData(UDPConnector udpSource) {
            if (udpSource == null) return;
            OIMSG msg = udpSource.GetNewData();

            while (msg != null && msg.data != null && msg.data.Length > 0) {
                // Make sure there is data in the stream.
                int packetID = -1;
                using (MemoryStream str = new MemoryStream(msg.data)) {
                    using (BinaryReader reader = new BinaryReader(str)) {
                        packetID = reader.ReadInt32();
                    }
                }

                if (packetID == 8)  // current meshes list  /// remove all meshes with ids that are not in this list
                {
                    Leap.Frame newFrame = LeapSerializer.Deserialize(msg.data);

                    if (newFrame != null) {
                        _CurrentFrame = newFrame;
                    }
                }
                msg = udpSource.GetNewData();
            }
        }


        public Leap.Frame CurrentFrame {
            get {
                return _CurrentFrame;
            }
        }

        public Leap.Frame CurrentFixedFrame {
            get {
                return _CurrentFrame;
            }
        }
    }
}