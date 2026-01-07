using System.IO;
using System.Text;
using System.Globalization;
using UnityEngine;

public class DataExporter : MonoBehaviour {

    public void ConvertArrayToCsv(float[,] dataArray) {
        string filePath = "C:\\Users\\legra\\OneDrive\\Documents\\UFU\\project\\CodesUnity\\Ski3D\\Assets\\Data" + "/" + "rollMovement2" + ".csv";
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < dataArray.GetLength(0); i++) // Iterate through rows
        {
            System.Collections.Generic.List<string> rowValues = new System.Collections.Generic.List<string>();
            for (int j = 0; j < dataArray.GetLength(1); j++) // Iterate through columns
            {
                rowValues.Add(dataArray[i, j].ToString(CultureInfo.InvariantCulture));
            }
            sb.AppendLine(string.Join(",", rowValues));
        }
        File.WriteAllText(filePath, sb.ToString());
        Debug.Log("CSV file saved to: " + filePath);
    }

}
