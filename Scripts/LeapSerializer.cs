// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using SysDiag = System.Diagnostics;
using System.IO;
using UnityEngine;
using Leap;

public static class LeapSerializer {

    public static byte[] Serialize(Frame frame) {
        byte[] data = null;

        using (MemoryStream stream = new MemoryStream()) {
            using (BinaryWriter writer = new BinaryWriter(stream)) {

                writer.Write(8); // '8' announces Leap frame packet

                writer.Write(frame.Id);
                writer.Write(frame.Timestamp);
                writer.Write(frame.CurrentFramesPerSecond);

                // interaction box
                WriteVector(writer, frame.InteractionBox.Size);
                WriteVector(writer, frame.InteractionBox.Center);

                // Hands
                writer.Write(frame.Hands.Count);

                foreach (Hand hand in frame.Hands) {
                    WriteHand(writer, hand);
                }

                stream.Position = 0;
                data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
            }
        }

        return data;
    }

    private static void WriteVector(BinaryWriter writer, Vector vector) {
        SysDiag.Debug.Assert(writer != null);

        writer.Write(vector.x);
        writer.Write(vector.y);
        writer.Write(vector.z);
    }

    private static void WriteLeapQuaternion(BinaryWriter writer, LeapQuaternion lq) {
        SysDiag.Debug.Assert(writer != null);

        writer.Write(lq.w);
        writer.Write(lq.x);
        writer.Write(lq.y);
        writer.Write(lq.z);
    }

    private static void WriteHand(BinaryWriter writer, Hand hand) {
        SysDiag.Debug.Assert(writer != null);

        writer.Write(hand.FrameId);
        writer.Write(hand.Id);
        writer.Write(hand.Confidence);
        writer.Write(hand.GrabStrength);
        writer.Write(hand.GrabAngle);
        writer.Write(hand.PinchStrength);
        writer.Write(hand.PinchDistance);
        writer.Write(hand.PalmWidth);
        writer.Write(hand.IsLeft);
        writer.Write(hand.TimeVisible);

        WriteArm(writer, hand.Arm);

        writer.Write(hand.Fingers.Count);
        foreach (Finger finger in hand.Fingers) {
            WriteFinger(writer, finger, hand.FrameId);
        }

        WriteVector(writer, hand.PalmPosition);
        WriteVector(writer, hand.StabilizedPalmPosition);
        WriteVector(writer, hand.PalmVelocity);
        WriteVector(writer, hand.PalmNormal);
        WriteLeapQuaternion(writer, hand.Rotation);
        WriteVector(writer, hand.Direction);
        WriteVector(writer, hand.WristPosition);

    }

    private static void WriteArm(BinaryWriter writer, Arm arm) {
        SysDiag.Debug.Assert(writer != null);

        WriteVector(writer, arm.ElbowPosition);
        WriteVector(writer, arm.WristPosition);
        WriteVector(writer, arm.Center);
        WriteVector(writer, arm.Direction);

        writer.Write(arm.Length);
        writer.Write(arm.Width);

        WriteLeapQuaternion(writer, arm.Rotation);
    }

    private static void WriteFinger(BinaryWriter writer, Finger finger, long frameId) {
        SysDiag.Debug.Assert(writer != null);

        writer.Write(frameId);
        writer.Write(finger.HandId);
        writer.Write(finger.Id - (finger.HandId * 10));
        writer.Write(finger.TimeVisible);

        WriteVector(writer, finger.TipPosition);
        WriteVector(writer, finger.TipVelocity);
        WriteVector(writer, finger.Direction);
        WriteVector(writer, finger.StabilizedTipPosition);

        writer.Write(finger.Width);
        writer.Write(finger.Length);
        writer.Write(finger.IsExtended);
        writer.Write((int)finger.Type);

        WriteBone(writer, finger.Bone((Bone.BoneType)0));
        WriteBone(writer, finger.Bone((Bone.BoneType)1));
        WriteBone(writer, finger.Bone((Bone.BoneType)2));
        WriteBone(writer, finger.Bone((Bone.BoneType)3));
    }

    private static void WriteBone(BinaryWriter writer, Bone bone) {
        SysDiag.Debug.Assert(writer != null);

        WriteVector(writer, bone.PrevJoint);
        WriteVector(writer, bone.NextJoint);
        WriteVector(writer, bone.Center);
        WriteVector(writer, bone.Direction);

        writer.Write(bone.Length);
        writer.Write(bone.Width);
        writer.Write((int)bone.Type);

        WriteLeapQuaternion(writer, bone.Rotation);
    }







    public static Frame Deserialize(byte[] data) {
        Frame reconstructedFrame = new Frame();

        using (MemoryStream stream = new MemoryStream(data)) {
            using (BinaryReader reader = new BinaryReader(stream)) {
                int packetID = reader.ReadInt32();
                if (packetID != 8) return null;

                long Id = reader.ReadInt64();
                long timestamp = reader.ReadInt64();
                float fps = reader.ReadSingle();

                Vector iSize = ReadVector(reader);
                Vector iCenter = ReadVector(reader);

                InteractionBox interactionBox = new InteractionBox(iCenter, iSize);

                int handsAm = reader.ReadInt32();
                List<Hand> hands = new List<Hand>();

                for (int i = 0; i < handsAm; i++) {
                    hands.Add(ReadHand(reader));
                }

                reconstructedFrame = new Frame(Id, timestamp, fps, interactionBox, hands);
            }
        }
        return reconstructedFrame;
    }


    private static Vector ReadVector(BinaryReader reader) {
        SysDiag.Debug.Assert(reader != null);

        Vector vector = new Vector();

        vector.x = reader.ReadSingle();
        vector.y = reader.ReadSingle();
        vector.z = reader.ReadSingle();

        return vector;
    }

    private static Hand ReadHand(BinaryReader reader) {
        SysDiag.Debug.Assert(reader != null);

        long frameId = reader.ReadInt64();
        int Id = reader.ReadInt32();
        float confidence = reader.ReadSingle();
        float grabStrength = reader.ReadSingle();
        float grabAngle = reader.ReadSingle();
        float pinchStrength = reader.ReadSingle();
        float pinchDistance = reader.ReadSingle();
        float palmWidth = reader.ReadSingle();
        bool isLeft = reader.ReadBoolean();
        float timeVisible = reader.ReadSingle();

        Arm arm = ReadArm(reader);

        int fingerAm = reader.ReadInt32();
        List<Finger> fingers = new List<Finger>();

        for (int i = 0; i < fingerAm; i++) {
            fingers.Add(ReadFinger(reader));
        }

        Vector palmPosition = ReadVector(reader);
        Vector stabilizedPalmPosition = ReadVector(reader);
        Vector palmVelocity = ReadVector(reader);
        Vector palmNormal = ReadVector(reader);
        LeapQuaternion palmOrientation = ReadLeapQuaternion(reader);
        Vector direction = ReadVector(reader);
        Vector wristPosition = ReadVector(reader);

        Hand hand = new Hand(frameId, Id, confidence, grabStrength, grabAngle, pinchStrength, pinchDistance, palmWidth, isLeft, timeVisible, arm, fingers, palmPosition, stabilizedPalmPosition, palmVelocity, palmNormal, palmOrientation, direction, wristPosition);

        return hand;
    }

    private static Arm ReadArm(BinaryReader reader) {
        SysDiag.Debug.Assert(reader != null);

        Vector elbow = ReadVector(reader);
        Vector wrist = ReadVector(reader);
        Vector center = ReadVector(reader);
        Vector direction = ReadVector(reader);

        float length = reader.ReadSingle();
        float width = reader.ReadSingle();

        LeapQuaternion rotation = ReadLeapQuaternion(reader);

        Arm arm = new Arm(elbow, wrist, center, direction, length, width, rotation);

        return arm;
    }

    private static LeapQuaternion ReadLeapQuaternion(BinaryReader reader) {
        SysDiag.Debug.Assert(reader != null);

        LeapQuaternion lq = new LeapQuaternion();

        lq.w = reader.ReadSingle();
        lq.x = reader.ReadSingle();
        lq.y = reader.ReadSingle();
        lq.z = reader.ReadSingle();

        return lq;
    }

    private static Finger ReadFinger(BinaryReader reader) {
        SysDiag.Debug.Assert(reader != null);

        long frameId = reader.ReadInt64();
        int handId = reader.ReadInt32();
        int fingerId = reader.ReadInt32();
        float timeVisible = reader.ReadSingle();

        Vector tipPosition = ReadVector(reader);
        Vector tipVelocity = ReadVector(reader);
        Vector direction = ReadVector(reader);
        Vector stabilizedTipPosition = ReadVector(reader);

        float width = reader.ReadSingle();
        float length = reader.ReadSingle();
        bool isExtended = reader.ReadBoolean();
        Finger.FingerType type = (Finger.FingerType)reader.ReadInt32();

        Bone metacarpal = ReadBone(reader);
        Bone proximal = ReadBone(reader);
        Bone intermediate = ReadBone(reader);
        Bone distal = ReadBone(reader);

        Finger finger = new Finger(frameId, handId, fingerId, timeVisible, tipPosition, tipVelocity, direction, stabilizedTipPosition, width, length, isExtended, type, metacarpal, proximal, intermediate, distal);

        return finger;
    }

    private static Bone ReadBone(BinaryReader reader) {
        SysDiag.Debug.Assert(reader != null);

        Vector prevJoint = ReadVector(reader);
        Vector nextJoint = ReadVector(reader);
        Vector center = ReadVector(reader);
        Vector direction = ReadVector(reader);

        float length = reader.ReadSingle();
        float width = reader.ReadSingle();

        Bone.BoneType type = (Bone.BoneType)reader.ReadInt32();

        LeapQuaternion rotation = ReadLeapQuaternion(reader);

        Bone bone = new Bone(prevJoint, nextJoint, center, direction, length, width, type, rotation);

        return bone;
    }

}