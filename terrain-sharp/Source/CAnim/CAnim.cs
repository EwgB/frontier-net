namespace terrain_sharp.Source.CAnim {
	using System.Collections.Generic;
	using System.Linq;

	using OpenTK;

	///<summary>Loads animations and applies them to models.</summary>
	class CAnim {
		public struct AnimJoint {
			public BoneId id;
			public Vector3 rotation;
		}

		public class AnimFrame {
			public List<AnimJoint> joint;
		}

		public List<AnimFrame> _frame;
		public AnimFrame _current;
		public AnimJoint GetFrame(float frame);
		public void SetDefaultAnimation();
		public int Frames { get { return _frame.Count; } } 
		public int Joints { get { return _frame[0].joint.Count; } }
		public BoneId Id(int frame, int index) { return _frame[frame].joint[index].id; }
		public Vector3 Rotation(int frame, int index) { return _frame[frame].joint[index].rotation; }
		public bool LoadBvh(string filename);
		public static BoneId BoneFromstring(string s);

		public static string NameFromBone(BoneId id) {
			switch (id) {
				case BoneId.Root:
					return "Root";
				case BoneId.Pelvis:
					return "Pelvis";
				case BoneId.RightHip:
					return "Hip Right";
				case BoneId.LeftHip:
					return "Hip Left";
				case BoneId.RightKnee:
					return "Knee Right";
				case BoneId.LeftKnee:
					return "Knee Left";
				case BoneId.RightAnkle:
					return "Ankle Right";
				case BoneId.LeftAnkle:
					return "Ankle Left";
				case BoneId.RightToe:
					return "Toe Right";
				case BoneId.LeftToe:
					return "Toe Left";
				case BoneId.Spine1:
				case BoneId.Spine2:
				case BoneId.Spine3:
					return "Spine";
				case BoneId.RightShoulder:
					return "Shoulder Right";
				case BoneId.LeftShoulder:
					return "Shoulder Left";
				case BoneId.RightArm:
					return "Arm Right";
				case BoneId.LeftArm:
					return "Arm Left";
				case BoneId.RightElbow:
					return "Elbow Right";
				case BoneId.LeftElbow:
					return "Elbow Left";
				case BoneId.RightWrist:
					return "Wrist Right";
				case BoneId.LeftWrist:
					return "Wrist Left";
				case BoneId.Neck:
					return "Neck";
				case BoneId.Head:
					return "Head";
				case BoneId.Face:
				case BoneId.Crown:
				case BoneId.Invalid:
					return "Bone Invalid";
			}
			return "Unknown";
		}

		BoneId BoneFromstring(string name) {
			if (name.Contains("Root"))
				return BoneId.Root;
			if (name.Contains("Pelvis"))
				return BoneId.Pelvis;
			if (name.Contains("HIP")) {
				if (name.Contains('L'))
					return BoneId.LeftHip;
				if (name.Contains('R'))
					return BoneId.RightHip;
				//Not left or right.  Probably actually the pelvis.
				return BoneId.Pelvis;
			}
			if (name.Contains("THIGH")) {
				if (name.Contains('L'))
					return BoneId.LeftHip;
				if (name.Contains('R'))
					return BoneId.RightHip;
				//Not left or right.  
				return BoneId.Invalid;
			}
			if (name.Contains("SHIN")) {
				if (name.IndexOf('L') >= 4)
					return BoneId.LeftKnee;
				if (name.IndexOf('R') >= 4)
					return BoneId.RightKnee;
				//Not left or right.  
				return BoneId.Invalid;
			}
			/*
			if (strstr (name, "KNEE")) {
				if (strchr (name + 4, 'L'))
					return BoneId.LeftKnee;
				if (strchr (name + 4, 'R'))
					return BoneId.RightKnee;
				//Not left or right.  
				return BoneId.Invalid;
			}
			*/

			if (name.Contains("FOOT")) {
				if (name.IndexOf('L') >= 4)
					return BoneId.LeftAnkle;
				if (name.IndexOf('R') >= 4)
					return BoneId.RightAnkle;
				//Not left or right.  
				return BoneId.Invalid;
			}

			/*
			if (strstr (name, "ANKLE")) {
				if (strchr (name + 5, 'L'))
					return BoneId.LeftAnkle;
				if (strchr (name + 5, 'R'))
					return BoneId.RightAnkle;
				//Not left or right.  
				return BoneId.Invalid;
			}
			*/
			if (name.Contains("BACK"))
				return BoneId.Spine1;
			if (name.Contains("Spine"))
				return BoneId.Spine1;
			if (name.Contains("Neck"))
				return BoneId.Neck;
			if (name.Contains("Head"))
				return BoneId.Head;
			if (name.Contains("SHOULDER")) {
				if (name.IndexOf('L') >= 8)
					return BoneId.LeftShoulder;
				if (name.IndexOf('R') >= 8)
					return BoneId.RightShoulder;
				//Not left or right? That can't be right.
				return BoneId.Invalid;
			}
			if (name.Contains("FOREARM")) {
				if (name.IndexOf('L') >= 7)
					return BoneId.LeftElbow;
				if (name.IndexOf('R') >= 7)
					return BoneId.RightElbow;
				//Not left or right? That can't be right.
				return BoneId.Invalid;
			}
			if (name.Contains("UPPERARM")) {
				if (name.IndexOf('L') >= 8)
					return BoneId.LeftArm;
				if (name.IndexOf('R') >= 8)
					return BoneId.RightArm;
				//Not left or right? That can't be right.
				return BoneId.Invalid;
			}
			/*
			if (strstr (name, "ELBOW")) {
				if (strchr (name + 7, 'L'))
					return BoneId.LeftElbow;
				if (strchr (name + 7, 'R'))
					return BoneId.RightElbow;
				//Not left or right? That can't be right.
				return BoneId.Invalid;
			}
			*/
			if (name.Contains("TOE")) {
				if (name.Contains('L'))
					return BoneId.LeftToe;
				if (name.Contains('R'))
					return BoneId.RightToe;
				//Not left or right? That can't be right.
				return BoneId.Invalid;
			}
			if (name.Contains("HAND")) {
				if (name.Contains('L'))
					return BoneId.LeftWrist;
				if (name.Contains('R'))
					return BoneId.RightWrist;
				//Not left or right? That can't be right.
				return BoneId.Invalid;
			}
			if (name.Contains("FINGERS1")) {
				if (strchr(name + 7, 'L'))
					return BoneId.LFINGERS1;
				if (strchr(name + 7, 'R'))
					return BoneId.RFINGERS1;
				//Not left or right? That can't be right.
				return BoneId.Invalid;
			}
			if (name.Contains("FINGERS2")) {
				if (strchr(name + 7, 'L'))
					return BoneId.LFINGERS2;
				if (strchr(name + 7, 'R'))
					return BoneId.RFINGERS2;
				//Not left or right? That can't be right.
				return BoneId.Invalid;
			}
			if (name.Contains("THUMB1")) {
				if (name.Contains('L'))
					return BoneId.LTHUMB1;
				if (name.Contains('R'))
					return BoneId.RTHUMB1;
				//Not left or right? That can't be right.
				return BoneId.Invalid;
			}
			if (name.Contains("THUMB2")) {
				if (name.Contains('L'))
					return BoneId.LTHUMB2;
				if (name.Contains('R'))
					return BoneId.RTHUMB2;
				//Not left or right? That can't be right.
				return BoneId.Invalid;
			}
			return BoneId.Invalid;

		}

		//This makes a one-frame do-nothing animating, so we don't crash when an animating if missing.
		void SetDefaultAnimation() {

			AnimJoint joint;

			_frame.clear();
			_frame.resize(1);
			joint.id = BoneId.Pelvis;
			joint.rotation = glVector(0.0f, 0.0f, 0.0f);
			_frame[0].joint.push_back(joint);

		}

		bool LoadBvh(string filename) {

			bool done;
			long size;
			string buffer;
			string token;
			string find;
			vector<BoneId> dem_bones;
			int channels;
			int frames;
			int frame;
			int bone;
			BoneId current_id;
			AnimJoint joint;
			string path;

			path.assign("anims//");
			path.append(filename);
			if (!strchr(filename, '.'))
				path.append(".bvh");
			buffer = FileLoad((string) path.c_str(), &size);
			if (!buffer) {
				ConsoleLog("LoadBvh: Can't find %s", (string) path.c_str());
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
						dem_bones.push_back(BoneId.Invalid);
					dem_bones.push_back(current_id);
				}
				if (find = strstr(token, "JOINT")) {
					find += 5; //skip the word joint
					current_id = BoneFromstring(find);
				}
				if (find = strstr(token, "MOTION")) {//we've reached the final section of the file
					token = strtok(NULL, NEWLINE);
					frames = 0;
					if (find = strstr(token, "FRAMES"))
						frames = atoi(find + 7);
					_frame.Clear();
					_frame.resize(frames);
					token = strtok(NULL, NEWLINE);//throw away "frame time" line.
					for (frame = 0; frame < frames; frame++) {
						token = strtok(NULL, NEWLINE);
						find = token;
						for (bone = 0; bone < dem_bones.Count; bone++) {
							joint.id = dem_bones[bone];
							joint.rotation.x = (float) atof(find);
							find = strchr(find, 32) + 1;
							joint.rotation.y = -(float) atof(find);
							find = strchr(find, 32) + 1;
							joint.rotation.z = -(float) atof(find);
							find = strchr(find, 32) + 1;
							if (joint.id != BoneId.Invalid) {
								_frame[frame].joint.push_back(joint);
							}
						}
					}
					done = true;
				}
				token = strtok(NULL, NEWLINE);
			}
			free(buffer);
			return true;

		}

		AnimJoint* GetFrame(float delta) {

			int i;
			int frame;
			int next_frame;
			AnimJoint aj;

			delta *= (float) Frames();
			frame = (int) delta % Frames();
			next_frame = (frame + 1) % Frames();
			delta -= (float) frame;
			_current.joint.clear();
			for (i = 0; i < Joints(); i++) {
				aj.id = _frame[frame].joint[i].id;
				aj.rotation = glVectorInterpolate(_frame[frame].joint[i].rotation, _frame[next_frame].joint[i].rotation, delta);
				_current.joint.push_back(aj);
			}
			return &_current.joint[0];

		}

	}
}
