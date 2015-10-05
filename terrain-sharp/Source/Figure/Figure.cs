namespace terrain_sharp.Source.Figure {
	using System;
	using System.Collections.Generic;

	using OpenTK;
	using OpenTK.Graphics.OpenGL;

	using Anim;
	using GLTypes;

	class Figure {
		private static readonly List<Bone> boneList = new List<Bone>() {
			new Bone() { pos = new Vector3(0, 0, 0), id = BoneId.BONE_ROOT, id_parent = BoneId.BONE_ROOT },
      new Bone() { pos = new Vector3(0, 0, 1.1f), id = BoneId.BONE_PELVIS, id_parent = BoneId.BONE_ROOT },
			new Bone() { pos = new Vector3(0.1f, 0, 1), id = BoneId.BONE_RHIP, id_parent = BoneId.BONE_PELVIS },
			new Bone() { pos = new Vector3(0.1f, 0, 0.5f), id = BoneId.BONE_RKNEE, id_parent = BoneId.BONE_RHIP },
			new Bone() { pos = new Vector3(0.1f, 0, 0), id = BoneId.BONE_RANKLE, id_parent = BoneId.BONE_RKNEE },
			new Bone() { pos = new Vector3(0.1f,-0.1f, 0), id = BoneId.BONE_RTOE, id_parent = BoneId.BONE_RANKLE },

			new Bone() { pos = new Vector3(-0.1f, 0, 1), id = BoneId.BONE_LHIP, id_parent = BoneId.BONE_PELVIS },
			new Bone() { pos = new Vector3(-0.1f, 0, 0.5f), id = BoneId.BONE_LKNEE, id_parent = BoneId.BONE_LHIP },
			new Bone() { pos = new Vector3(-0.1f, 0, 0), id = BoneId.BONE_LANKLE, id_parent = BoneId.BONE_LKNEE },
			new Bone() { pos = new Vector3(-0.1f,-0.1f, 0), id = BoneId.BONE_LTOE, id_parent = BoneId.BONE_LANKLE },

			new Bone() { pos = new Vector3(0, 0, 1.55f), id = BoneId.BONE_SPINE1, id_parent = BoneId.BONE_PELVIS },

			new Bone() { pos = new Vector3(0.1f, 0, 1.5f), id = BoneId.BONE_RSHOULDER, id_parent = BoneId.BONE_SPINE1 },
			new Bone() { pos = new Vector3(0.2f, 0, 1.5f), id = BoneId.BONE_RARM, id_parent = BoneId.BONE_RSHOULDER },
			new Bone() { pos = new Vector3(0.4f, 0, 1.5f), id = BoneId.BONE_RELBOW, id_parent = BoneId.BONE_RARM },
			new Bone() { pos = new Vector3(0.8f, 0, 1.5f), id = BoneId.BONE_RWRIST, id_parent = BoneId.BONE_RELBOW },

			new Bone() { pos = new Vector3(-0.1f, 0, 1.5f), id = BoneId.BONE_LSHOULDER, id_parent = BoneId.BONE_SPINE1 },
			new Bone() { pos = new Vector3(-0.2f, 0, 1.5f), id = BoneId.BONE_LARM, id_parent = BoneId.BONE_LSHOULDER },
			new Bone() { pos = new Vector3(-0.4f, 0, 1.5f), id = BoneId.BONE_LELBOW, id_parent = BoneId.BONE_LARM },
			new Bone() { pos = new Vector3(-0.8f, 0, 1.5f), id = BoneId.BONE_LWRIST, id_parent = BoneId.BONE_LELBOW },

			new Bone() { pos = new Vector3(0, 0, 1.6f), id = BoneId.BONE_NECK, id_parent = BoneId.BONE_SPINE1 },
			new Bone() { pos = new Vector3(0, 0, 1.65f), id = BoneId.BONE_HEAD, id_parent = BoneId.BONE_NECK },
			new Bone() { pos = new Vector3(0,-0.2f, 1.65f), id = BoneId.BONE_FACE, id_parent = BoneId.BONE_HEAD },
			new Bone() { pos = new Vector3(0, 0, 1.8f), id = BoneId.BONE_CROWN, id_parent = BoneId.BONE_FACE }
		};

		private static readonly Vector3 UP = new Vector3(0, 0, 1);

		private CFigure fig;
		private CFigure fig2;
		private CAnim anim;
		private CAnim anim_stand;

		private int frame;
		private bool moveit = true;
		private bool stand;

		private float figureRender_nn;

		private void add_hull(CFigure f, Vector3 p, float d, float h, BoneId id) {
			Mesh m = f.Skin();
			int offset = m.Vertices.Count;
			m.PushVertex(new Vector3(p.X, p.Y, p.Z), UP, new Vector2());
			m.PushVertex(new Vector3(p.X, p.Y + d, p.Z), UP, new Vector2());
			m.PushVertex(new Vector3(p.X, p.Y + d, p.Z + h), UP, new Vector2());
			m.PushVertex(new Vector3(p.X, p.Y, p.Z + h), UP, new Vector2());
			m.PushQuad(offset + 0, offset + 1, offset + 2, offset + 3);
			m.PushQuad(offset + 3, offset + 2, offset + 1, offset + 0);
			f.PushWeight(id, offset, 1.0f);
			f.PushWeight(id, offset + 1, 1.0f);
			f.PushWeight(id, offset + 2, 1.0f);
			f.PushWeight(id, offset + 3, 1.0f);
		}

		public void FigureInit() {
			boneList.ForEach(bone => fig.PushBone(bone));
			Mesh skin = fig.Skin();

			add_hull(fig, new Vector3(0.1f, 0.05f, 0.5f), -0.1f, -0.4f, BoneId.BONE_RKNEE);
			add_hull(fig, new Vector3(-0.1f, 0.05f, 0.5f), -0.1f, -0.4f, BoneId.BONE_LKNEE);

			add_hull(fig, new Vector3(0.1f, 0.05f, 1.0f), -0.1f, -0.5f, BoneId.BONE_RHIP);
			add_hull(fig, new Vector3(-0.1f, 0.05f, 1.0f), -0.1f, -0.5f, BoneId.BONE_LHIP);

			add_hull(fig, new Vector3(0.2f, 0.05f, 1.5f), -0.1f, -0.1f, BoneId.BONE_RSHOULDER);
			add_hull(fig, new Vector3(0.5f, 0.05f, 1.5f), -0.1f, -0.1f, BoneId.BONE_RELBOW);

			add_hull(fig, new Vector3(-0.2f, 0.05f, 1.5f), -0.1f, -0.1f, BoneId.BONE_LSHOULDER);
			add_hull(fig, new Vector3(-0.5f, 0.05f, 1.5f), -0.1f, -0.1f, BoneId.BONE_LELBOW);

			fig.Prepare();
			anim.LoadBvh("Anims//run.bvh");
			anim_stand.LoadBvh("Anims//stand.bvh");

			fig2.LoadX("models//male.x");
			//  fig2.BoneInflate (BoneId.BONE_HEAD, 0.01f);
			/*
			{
				FILE*             file;
				file = fopen ("stand.bvh", "w+b");
				if (!file) 
					return;
				for (i = 0; i < fig._bone.size (); i++) {
					fprintf (file, "Joint %s\n", CAnim::NameFromBone (fig._bone[i]._id));
					fprintf (file, "CHANNELS 3\n");
				}
				fprintf (file, "Motion\n");
				fprintf (file, "Frames: 1\n");
				fprintf (file, "Frame Time: 1.0\n");
				for (i = 0; i < fig._bone.size (); i++) 
					fprintf (file, "0.0 0.0 0.0 ");
				fprintf (file, "\n");
				fclose (file);
			}
			*/
		}

		public void FigureRender() {
			if (moveit)
				figureRender_nn += 0.03f;

			fig2.RotateBone(BoneId.BONE_SPINE1, new Vector3(0, 0, (float) Math.Sin(figureRender_nn * 3) * 25.0f));
			fig2.RotateBone(BoneId.BONE_RFINGERS1, new Vector3(0, (float) -Math.Abs(Math.Sin(figureRender_nn * 1)) * -80, 0));
			//fig2.RotateBone (BoneId.BONE_RELBOW, new Vector3 ((float) Math.Abs (Math.Cos (figureRender_nn * 1)) * 45.0f, 0, 0));
			//fig2.RotateBone (BoneId.BONE_LSHOULDER, new Vector3 (0, (float) Math.Abs (Math.Sin (figureRender_nn * 3)) * 80, 0));

			//fig2.RotateBone (BoneId.BONE_LELBOW, new Vector3 (0, 0, (float) Math.Abs (Math.Cos (figureRender_nn * 2)) * 90));
			fig2.RotateBone(BoneId.BONE_LWRIST, new Vector3(0, (float) Math.Abs(Math.Cos(figureRender_nn * 2)) * 90, 0));
			//fig2.RotateBone (BoneId.BONE_RHIP, new Vector3 ((float) Math.Sin (figureRender_nn) * 25.0f, 0,  0));
			//fig2.RotateBone (BoneId.BONE_RKNEE, new Vector3 ((float) -Math.Abs (Math.Cos (figureRender_nn * 2) * 45.0f), 0,  0));

			/*
			for (int i = 0; i < anim._frame[frame].joint.size (); i++) {
				//if (anim._frame[frame].joint[i].id > BoneId.BONE_PELVIS)
					fig.RotateBone (anim._frame[frame].joint[i].id, anim._frame[frame].joint[i].rotation);
			}
			*/
			if (stand) {
				//fig.Animate (&anim_stand, figureRender_nn);
				//fig2.Animate (&anim_stand, figureRender_nn);
			} else {
				//fig.Animate (&anim, figureRender_nn);
				//fig2.Animate (&anim, figureRender_nn);
			}
			frame++;
			//frame %= anim._frame.size ();
			//fig.Update ();
			//fig2.Update ();
			if (InputKeyPressed(SDLK_f)) {
				fig.PositionSet(AvatarPosition() + new Vector3(0, -2.0f, 0));
				fig2.PositionSet(AvatarPosition() + new Vector3(0, 2.0f, 0));
			}
			if (InputKeyPressed(SDLK_g))
				moveit = !moveit;
			if (InputKeyPressed(SDLK_h))
				stand = !stand;

			GL.BindTexture(TextureTarget.Texture2D, 0);
			//GL.Disable (GL_LIGHTING);
			//fig.Render ();
			//fig2.Render ();
			//GL.Enable (GL_LIGHTING);
		}
	}
}