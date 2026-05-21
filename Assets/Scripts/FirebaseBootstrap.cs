using System.Threading.Tasks;
using Firebase;
using UnityEngine;

public class FirebaseBootstrap : MonoBehaviour
{
    private static readonly Task<DependencyStatus> ReadyTask = Task.FromResult(DependencyStatus.Available);
    public static Task<DependencyStatus> InitTask => ReadyTask;
}
