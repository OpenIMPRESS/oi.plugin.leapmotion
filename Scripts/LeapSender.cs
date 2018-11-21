using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Leap;
using Leap.Unity;
using oi.core.network;

namespace oi.plugin.leapmotion {
    public class LeapSender : MonoBehaviour {
        public LeapServiceProvider provider;
        public UDPConnector udpConnector;

        // Use this for initialization
        void Start() {
            //provider = GetComponent<LeapProvider>();
        }

        // Update is called once per frame
        void Update() {
            Frame frame = provider.CurrentFrame;
            if (frame != null) {
                byte[] serialized = LeapSerializer.Serialize(frame);
                udpConnector.SendData(serialized);
            }
        }
    }
}