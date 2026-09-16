using UnityEngine;
using UnityEngine.SceneManagement;

public class MainmenuHandler : MonoBehaviour
{
   public void SimulationAndDriftMode()
   {
      SceneManager.LoadScene("Simulation And Drift Mode");
   }

   public void Quit()
   {
      Application.Quit();
   }
}
