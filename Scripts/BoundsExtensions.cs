using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BoundsExtensions {

    public static void EncapsulateTransformedBounds(ref this Bounds sourceBounds, Bounds targetBounds) {
        sourceBounds.EncapsulateTransformedBounds(Matrix4x4.identity, targetBounds);
	}

    public static void EncapsulateTransformedBounds(ref this Bounds sourceBounds, Matrix4x4 targetMatrix, Bounds targetBounds) {
        Vector3[] boundsCorners = {
            new Vector3(targetBounds.min.x, targetBounds.min.y, targetBounds.min.z),
            new Vector3(targetBounds.max.x, targetBounds.min.y, targetBounds.min.z),
            new Vector3(targetBounds.min.x, targetBounds.max.y, targetBounds.min.z),
            new Vector3(targetBounds.max.x, targetBounds.max.y, targetBounds.min.z),
            new Vector3(targetBounds.min.x, targetBounds.min.y, targetBounds.max.z),
            new Vector3(targetBounds.max.x, targetBounds.min.y, targetBounds.max.z),
            new Vector3(targetBounds.min.x, targetBounds.max.y, targetBounds.max.z),
            new Vector3(targetBounds.max.x, targetBounds.max.y, targetBounds.max.z)
        };
        foreach (Vector3 boundsCorner in boundsCorners) {
            //Debug.DrawLine(targetMatrix.MultiplyPoint(boundsCorner), targetMatrix.MultiplyPoint(boundsCorner)+Vector3.up, Color.blue, 1f);
            sourceBounds.Encapsulate(targetMatrix.MultiplyPoint(boundsCorner));
        }
	}

}
