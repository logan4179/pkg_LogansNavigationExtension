using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_TestTriDiamond : TDG_base
    {

        [Header("GIZMOS")]
        public Color Color_diamondLines;


        public bool amSelected;
		protected override void OnDrawGizmos()
		{
            base.OnDrawGizmos();

			if( Selection.activeGameObject != gameObject )
            {
                return;
            }

            for ( int i = 0; i < _mgr.Triangles.Length; i++ )
            {
				drawDiamondGizmos(_mgr.Triangles[i], _mgr.Triangles[i].IsWithinTriangleDiamond(transform.position));
			}
		}

        private void drawDiamondGizmos( LNX_Triangle tri, bool amFocused )
        {
            GUIStyle gstl = GUIStyle.none;

            if ( amFocused )
            {
                Gizmos.color = Color.yellow;
                gstl.normal.textColor = Color.yellow;
            }
            else
            {
                Gizmos.color = Color_diamondLines;
            }


			//Vector3 v_to = Quaternion.AngleAxis(tri.Verts[0].Angle * 0.5f, -Vector3.Cross(tri.Verts[0].v_normal, tri.Verts[0].v_toCenter)) * tri.Verts[0].v_toCenter;
			Vector3 v_to = Quaternion.AngleAxis(tri.Verts[0].AngleAtBend * 0.5f, -Vector3.Cross(tri.Verts[0].v_normal, tri.Verts[0].v_toCenter)) * tri.Verts[0].v_toCenter;

			Gizmos.DrawLine(tri.Verts[0].Position, tri.Verts[0].Position + (v_to * tri.Verts[0].DistanceToCenter) );

			//v_to = Quaternion.AngleAxis(tri.Verts[1].Angle * 0.5f, -Vector3.Cross(tri.Verts[1].v_normal, tri.Verts[1].v_toCenter)) * tri.Verts[1].v_toCenter;
			v_to = Quaternion.AngleAxis(tri.Verts[1].AngleAtBend * 0.5f, -Vector3.Cross(tri.Verts[1].v_normal, tri.Verts[1].v_toCenter)) * tri.Verts[1].v_toCenter;
			Gizmos.DrawLine(tri.Verts[1].Position, tri.Verts[1].Position + (v_to * tri.Verts[1].DistanceToCenter));

			//v_to = Quaternion.AngleAxis(tri.Verts[2].Angle * 0.5f, -Vector3.Cross(tri.Verts[2].v_normal, tri.Verts[2].v_toCenter)) * tri.Verts[2].v_toCenter;
			v_to = Quaternion.AngleAxis(tri.Verts[2].AngleAtBend * 0.5f, -Vector3.Cross(tri.Verts[2].v_normal, tri.Verts[2].v_toCenter)) * tri.Verts[2].v_toCenter;
			Gizmos.DrawLine(tri.Verts[2].Position, tri.Verts[2].Position + (v_to * 2f));
            
		}
	}
}