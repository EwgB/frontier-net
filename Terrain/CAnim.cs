/*-----------------------------------------------------------------------------
  CAnim.cpp
-------------------------------------------------------------------------------
  Loads animations and applies them to models (CFigures)
-----------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Frontier {
	#region Structs and enums
	enum BoneId {
		Root, Pelvis, RHip, LHip, RKnee, LKnee, RAnkle, LAnkle, RToe, LToe,
		Spine1, Spine2, Spine3, RArm, LArm, RShoulder, LShoulder, RElbow, LElbow, RWrist,
		LWrist, RFingers1, LFingers1, RFingers2, LFingers2, RThumb1, LThumb1, RThumb2, LThumb2, Neck,
		Head, Face, Crown, Unknown0, Unknown1, Unknown2, Unknown3, Unknown4, Unknown5, Unknown6,
		Unknown7, Unknown8, Unknown9, Unknown10, Unknown11, Unknown12, Unknown13, Unknown14, Unknown15, Invalid
	}

	struct AnimJoint {
		public BoneId id;
		public Vector3 rotation;
	}

	struct AnimFrame { public List<AnimJoint> joint; }
	#endregion

	class CAnim {
		#region Member fields and properties
		public const int BoneIdCount = 49;

		public List<AnimFrame> mFrame;
		public AnimFrame       mCurrent;

		public int FrameCount { get { return mFrame.Count; } }
		public int JointCount { get { return mFrame[0].joint.Count; } }
		#endregion

		#region Public methods
		public int Id(int frame, int index) { return (int) mFrame[frame].joint[index].id; }
		public Vector3 Rotation(int frame, int index) { return mFrame[frame].joint[index].rotation; }

		public static string NameFromBone(BoneId id) {
			switch (id) {
				case BoneId.Root: return "Root";
				case BoneId.Pelvis: return "Pelvis";
				case BoneId.RHip: return "Hip Right";
				case BoneId.LHip: return "Hip Left";
				case BoneId.RKnee: return "Knee Right";
				case BoneId.LKnee: return "Knee Left";
				case BoneId.RAnkle: return "Ankle Right";
				case BoneId.LAnkle: return "Ankle Left";
				case BoneId.RToe: return "Toe Right";
				case BoneId.LToe: return "Toe Left";
				case BoneId.Spine1:
				case BoneId.Spine2:
				case BoneId.Spine3: return "Spine";
				case BoneId.RShoulder: return "Shoulder Right";
				case BoneId.LShoulder: return "Shoulder Left";
				case BoneId.RArm: return "Arm Right";
				case BoneId.LArm: return "Arm Left";
				case BoneId.RElbow: return "Elbow Right";
				case BoneId.LElbow: return "Elbow Left";
				case BoneId.RWrist: return "Wrist Right";
				case BoneId.LWrist: return "Wrist Left";
				case BoneId.Neck: return "Neck";
				case BoneId.Head: return "Head";
				case BoneId.Face:
				case BoneId.Crown:
				case BoneId.Invalid: return "Bone Invalid";
			}
			return "Unknown";
		}

		public static BoneId BoneFromString(string name) {
			if (name.Contains("ROOT")) return BoneId.Root;
			if (name.Contains("PELVIS")) return BoneId.Pelvis;
			if (name.Contains("BACK")) return BoneId.Spine1;
			if (name.Contains("SPINE")) return BoneId.Spine1;
			if (name.Contains("NECK")) return BoneId.Neck;
			if (name.Contains("HEAD")) return BoneId.Head;

			if (name.Contains("HIP")) {
				if (name.LastIndexOf('L') > 0) return BoneId.LHip;
				if (name.LastIndexOf('R') > 0) return BoneId.RHip;
				// Not left or right. Probably actually the pelvis.
				return BoneId.Pelvis;
			} else if (name.Contains("THIGH")) {
				if (name.LastIndexOf('L') > 0) return BoneId.LHip;
				if (name.LastIndexOf('R') > 0) return BoneId.RHip;
				// Not left or right.  
				return BoneId.Invalid;
			} else if (name.Contains("SHIN")) {
				if (name.LastIndexOf('L') > 0) return BoneId.LKnee;
				if (name.LastIndexOf('R') > 0) return BoneId.RKnee;
				// Not left or right.  
				return BoneId.Invalid;
			} else if (name.Contains("FOOT")) {
				if (name.LastIndexOf('L') > 0) return BoneId.LAnkle;
				if (name.LastIndexOf('R') > 0) return BoneId.RAnkle;
				// Not left or right.  
				return BoneId.Invalid;
			} else if (name.Contains("SHOULDER")) {
				if (name.LastIndexOf('L') > 7) return BoneId.LShoulder;
				if (name.LastIndexOf('R') > 7) return BoneId.RShoulder;
				// Not left or right? That can't be right.
				return BoneId.Invalid;
			} else if (name.Contains("FOREARM")) {
				if (name.LastIndexOf('L') > 0) return BoneId.LElbow;
				if (name.LastIndexOf('R') > 7) return BoneId.RElbow;
				// Not left or right? That can't be right.
				return BoneId.Invalid;
			} else if (name.Contains("UPPERARM")) {
				if (name.LastIndexOf('L') > 0) return BoneId.LArm;
				if (name.LastIndexOf('R') > 7) return BoneId.RArm;
				// Not left or right? That can't be right.
				return BoneId.Invalid;
			} else if (name.Contains("TOE")) {
				if (name.LastIndexOf('L') > 0) return BoneId.LToe;
				if (name.LastIndexOf('R') > 0) return BoneId.RToe;
				// Not left or right? That can't be right.
				return BoneId.Invalid;
			} else if (name.Contains("HAND")) {
				if (name.LastIndexOf('L') > 0) return BoneId.LWrist;
				if (name.LastIndexOf('R') > 0) return BoneId.RWrist;
				//Not left or right? That can't be right.
				return BoneId.Invalid;
			} else if (name.Contains("FINGERS1")) {
				if (name.LastIndexOf('L') > 0) return BoneId.LFingers1;
				if (name.LastIndexOf('R') > 7) return BoneId.RFingers1;
				// Not left or right? That can't be right.
				return BoneId.Invalid;
			} else if (name.Contains("FINGERS2")) {
				if (name.LastIndexOf('L') > 0) return BoneId.LFingers2;
				if (name.LastIndexOf('R') > 7) return BoneId.RFingers2;
				// Not left or right? That can't be right.
				return BoneId.Invalid;
			} else if (name.Contains("THUMB1")) {
				if (name.LastIndexOf('L') > 0) return BoneId.LThumb1;
				if (name.LastIndexOf('R') > 0) return BoneId.RThumb1;
				// Not left or right? That can't be right.
				return BoneId.Invalid;
			} else if (name.Contains("THUMB2")) {
				if (name.LastIndexOf('L') > 0) return BoneId.LThumb2;
				if (name.LastIndexOf('R') > 0) return BoneId.RThumb2;
				// Not left or right? That can't be right.
				return BoneId.Invalid;
			}

			//if (strstr (name, "KNEE")) {
			//  if (strchr (name + 4, 'L'))		return LKnee;
			//  if (strchr (name + 4, 'R'))		return RKnee;
			//  //Not left or right.  
			//  return Invalid;
			//}

			//if (strstr (name, "ANKLE")) {
			//  if (strchr (name + 5, 'L'))		return LAnkle;
			//  if (strchr (name + 5, 'R'))		return RAnkle;
			//  //Not left or right.  
			//  return Invalid;
			//}

			//if (strstr (name, "ELBOW")) {
			//  if (strchr (name + 7, 'L'))		return LElbow;
			//  if (strchr (name + 7, 'R'))		return RElbow;
			//  //Not left or right? That can't be right.
			//  return Invalid;
			//}

			return BoneId.Invalid;
		}

		// This makes a one-frame do-nothing animating, so we don't crash when an animation if missing.
		public void SetDefaultAnimation() {
			mFrame.Clear();
			mFrame.Capacity = 1;

			AnimJoint joint;
			joint.id = BoneId.Pelvis;
			joint.rotation = Vector3.Zero;
			mFrame[0].joint.Add(joint);
		}

		public bool LoadBvh(string filename) {
			string path = "anims//" + filename;
			if (filename.LastIndexOf('.') == -1)
				path += ".bvh";

			//bool            done;
			//long            size;
			//char*           buffer;
			//char*           token;
			//char*           find;
			//List<BoneId>  dem_bones;
			//int             channels;
			//int        frames;
			//int        frame;
			//int        bone;
			//BoneId          current_id;
			//AnimJoint       joint;

			buffer = FileLoad(path, size);
			if (!buffer) {
				//ConsoleLog("LoadBvh: Can't find %s", (char*) path.c_str());
				SetDefaultAnimation();
				return false;
			}
			_strupr(buffer);
			done = false;
			channels = 3;
			current_id = BoneId.Invalid;
			token = strtok(buffer, NEWLINE);

			while (!done) {
				if (find = strstr(token, "CHANNEL")) {
					channels = atoi(find + 8);
					//Six channels means first 3 are position.  Ignore.
					if (channels == 6)
						dem_bones.Add(BoneId.Invalid);
					dem_bones.Add(current_id);
				}
				if (find = strstr(token, "JOINT")) {
					find += 5; //skip the word joint
					current_id = BoneFromString(find);
				}
				if (find = strstr(token, "MOTION")) {//we've reached the final section of the file
					token = strtok(null, NEWLINE);
					frames = 0;
					if (find = strstr(token, "FRAMES"))
						frames = atoi(find + 7);
					mFrame.Clear();
					mFrame.Capacity = frames;
					token = strtok(null, NEWLINE);//throw away "frame time" line.
					for (int frame = 0; frame < frames; frame++) {
						token = strtok(null, NEWLINE);
						find = token;
						for (bone = 0; bone < dem_bones.size(); bone++) {
							joint.id = dem_bones[bone];
							joint.rotation.x = (float) atof(find);
							find = strchr(find, 32) + 1;
							joint.rotation.y = -(float) atof(find);
							find = strchr(find, 32) + 1;
							joint.rotation.z = -(float) atof(find);
							find = strchr(find, 32) + 1;
							if (joint.id != BoneId.Invalid) {
								mFrame[frame].joint.Add(joint);
							}
						}
					}
					done = true;
				}
				token = strtok(null, NEWLINE);
			}
			free(buffer);
			return true;
		}

		public List<AnimJoint> GetFrame(float delta) {
			int    frame;
			int    next_frame;
			AnimJoint   aj;

			delta *= (float) FrameCount;
			frame = (int) delta % FrameCount;
			next_frame = (frame + 1) % FrameCount;
			delta -= (float) frame;
			mCurrent.joint.Clear();
			for (int i = 0; i < JointCount; i++) {
				aj.id = mFrame[frame].joint[i].id;
				aj.rotation = glVectorInterpolate(mFrame[frame].joint[i].rotation, mFrame[next_frame].joint[i].rotation, delta);
				mCurrent.joint.Add(aj);
			}
			return mCurrent.joint;
		}
		#endregion
	}
}