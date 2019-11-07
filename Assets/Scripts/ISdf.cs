using UnityEngine;

public interface ISdf
{
    double Eval(double x, double y, double z);
    Vector3 Min();
    Vector3 Max();
}
