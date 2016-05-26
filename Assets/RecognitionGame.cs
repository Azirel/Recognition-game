using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using PDollarGestureRecognizer;

public class RecognitionGame : MonoBehaviour
{
    public float startTime = 15;
    public float patternShowTime = 4;
    public float pointInLineLifeTime = 0.3f;
    public float passCoef = 0.91f;
    public float distanceBetweenPointsInPixels = 2;
    public float timeDevider = 1.1f;
    private event EventHandler startEvent;
    private event EventHandler updateEvent;
    private event EventHandler startButtonPressEvent;
    private event EventHandler addNewPatternButtonPressEvent;
    private List<Gesture> gestureLibrary;
    private LineRenderer linesample;
    private LineRenderer lineToShowPattern;
    private UILabel stats;
    private List<Vector3> currentPositions;
    private List<LineRenderer> lines;
    private List<List<Vector3>> linesPositions;
    private UILabel timerLabel;
    private UILabel timeLabel;
    private float currentTime;
    private GameObject buttons;
    private List<Point> currentPointsToRecognize;
    private int score;
    private System.Random rand;
    private UILabel scoreLabel;
    private float totalDevider;
    private GameObject restartButton;
    private string patternToRecognize;
    private UIButton addButton;
    private UIInput patternNameInput;
    private LineRenderer lineForPatternAddition;
    private List<Vector3> positionsForPatternAddition;
    private List<Point> pointsForPatternAddition;
    private UILabel escLabel;
    private GameObject mainMenuParrent;
    private GameObject inGamParrent;
    private GameObject inPattenrAdditionParrent;

    #region Handlers
    void DrawingInPatternAddition(object sender, EventArgs args)
    {
        if (Input.GetMouseButtonDown(0) && Camera.main.ScreenToWorldPoint(Input.mousePosition).y > -0.7)
        {
            pointsForPatternAddition.Clear();
            positionsForPatternAddition.Clear();
            positionsForPatternAddition.Add(Camera.main.ScreenToWorldPoint(Input.mousePosition));
            positionsForPatternAddition[0] = new Vector3(positionsForPatternAddition[0].x, positionsForPatternAddition[0].y, 1f);
            lineForPatternAddition.SetVertexCount(1);
            pointsForPatternAddition.Add(new Point(Input.mousePosition.x, -Input.mousePosition.y, 0));
        }
        else if (Input.GetMouseButton(0) && Vector2.Distance(new Vector2(pointsForPatternAddition[pointsForPatternAddition.Count - 1].X, -pointsForPatternAddition[pointsForPatternAddition.Count - 1].Y), Input.mousePosition) >= 2 && Camera.main.ScreenToWorldPoint(Input.mousePosition).y > -0.7)
        {
            positionsForPatternAddition.Add(Camera.main.ScreenToWorldPoint(Input.mousePosition));
            positionsForPatternAddition[positionsForPatternAddition.Count - 1] = new Vector3(positionsForPatternAddition[positionsForPatternAddition.Count - 1].x, positionsForPatternAddition[positionsForPatternAddition.Count - 1].y, 1f);
            pointsForPatternAddition.Add(new Point(Input.mousePosition.x, -Input.mousePosition.y, 0));
            lineForPatternAddition.SetVertexCount(positionsForPatternAddition.Count);
            lineForPatternAddition.SetPositions(positionsForPatternAddition.ToArray());
        }
    }
    void BackToMainMenuHandler(object sender, EventArgs args)
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            BackToMainMenu();
        }
    }
    void AddPressButtonHandler(object sender, EventArgs args)
    {
        Debug.Log("In add" + pointsForPatternAddition.Count + " : " + patternNameInput.value);
        if (pointsForPatternAddition.Count > 1 && patternNameInput.value != "")
        {
            string fileName = String.Format("{0}/{1}-{2}.xml", Application.persistentDataPath, patternNameInput.value, DateTime.Now.ToFileTime());
            Debug.Log("Added to - " + fileName);
            GestureIO.WriteGesture(pointsForPatternAddition.ToArray(), patternNameInput.value, fileName);
            gestureLibrary.Add(new Gesture(pointsForPatternAddition.ToArray(), patternNameInput.value));
        }
    }
    void AddNewPatternPressButtonHandler(object sender, EventArgs args)
    {
        mainMenuParrent.SetActive(false);
        inGamParrent.SetActive(false);
        inPattenrAdditionParrent.SetActive(true);
        updateEvent = null;
        updateEvent += BackToMainMenuHandler;
        updateEvent += DrawingInPatternAddition;
        lineForPatternAddition = Instantiate<GameObject>(linesample.gameObject).GetComponent<LineRenderer>();
        lineForPatternAddition.name = "LineForPatternAddition";
    }
    void StartButtonPressHandler(object sender, EventArgs args)
    {
        score = 0;
        totalDevider = 1;
        currentTime = startTime;
        ShowRandomPattern(null, null);
        restartButton.SetActive(false);
        mainMenuParrent.SetActive(false);
        inGamParrent.SetActive(true);
        inPattenrAdditionParrent.SetActive(false);
        scoreLabel.text = "0";
        StartCoroutine(Cleaner());
        updateEvent = null;
        updateEvent += BackToMainMenuHandler;
    }
    void TimeCheckHandler(object sender, EventArgs args)
    {
        currentTime -= Time.deltaTime;
        timerLabel.text = currentTime.ToString("00.00");
        if (currentTime <= 0)
        {
            timerLabel.text = "00.00";
            Flush();
            updateEvent = null;
            restartButton.SetActive(true);
            StopAllCoroutines();
        }
    }
    void InGameDrawHnadler(object sender, EventArgs args)
    {
        if (Input.GetMouseButtonDown(0))
        {
            lines.Add(Instantiate<GameObject>(linesample.gameObject).GetComponent<LineRenderer>());
            currentPointsToRecognize.Clear();
            currentPointsToRecognize.Add(new Point(Input.mousePosition.x, -Input.mousePosition.y, 0));
            linesPositions.Add(new List<Vector3>());
            currentPositions = linesPositions[linesPositions.Count - 1];
            currentPositions.Add(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (currentPointsToRecognize.Count > 1)
            {
                PDollarGestureRecognizer.Result result = PDollarGestureRecognizer.PointCloudRecognizer.Classify(new Gesture(currentPointsToRecognize.ToArray(), ""), gestureLibrary.ToArray());
                if (result.Score >= passCoef && result.GestureClass == patternToRecognize)
                {
                    scoreLabel.text = (++score).ToString();
                    totalDevider *= timeDevider;
                    ShowRandomPattern(null, null);
                }
                //stats.text = result.GestureClass + " " + result.Score;
            }
            currentPointsToRecognize.Clear();
        }
        else if (Input.GetMouseButton(0) && Vector2.Distance(new Vector2(currentPointsToRecognize[currentPointsToRecognize.Count - 1].X, -currentPointsToRecognize[currentPointsToRecognize.Count - 1].Y), Input.mousePosition) >= 2)
        {
            currentPositions.Add(Camera.main.ScreenToWorldPoint(Input.mousePosition));
            currentPositions[currentPositions.Count - 1] = new Vector3(currentPositions[currentPositions.Count - 1].x, currentPositions[currentPositions.Count - 1].y, 1f);
            lines[lines.Count - 1].SetVertexCount(linesPositions[linesPositions.Count - 1].Count);
            lines[lines.Count - 1].SetPositions(linesPositions[linesPositions.Count - 1].ToArray());
            currentPointsToRecognize.Add(new Point(Input.mousePosition.x, -Input.mousePosition.y, 0));
        }
    }
    void LoadPatternsHandler(object sender, EventArgs args)
    {
        TextAsset[] gesturesXml = Resources.LoadAll<TextAsset>("Gestures/");
        foreach (TextAsset gestureXml in gesturesXml)
            gestureLibrary.Add(GestureIO.ReadGestureFromXML(gestureXml.text));
    }
    void LoadCustomPatternsHandler(object sender, EventArgs args)
    {
        string[] filePaths = System.IO.Directory.GetFiles(Application.persistentDataPath, "*.xml");
        foreach (string filePath in filePaths)
            gestureLibrary.Add(GestureIO.ReadGestureFromFile(filePath));
    }
    void FieldsDefinitionHandler(object sender, EventArgs args)
    {
        gestureLibrary = new List<Gesture>();
        linesample = GameObject.Find("LineSample").GetComponent<LineRenderer>();
        lineToShowPattern = Instantiate<GameObject>(linesample.gameObject).GetComponent<LineRenderer>();
        lineToShowPattern.gameObject.name = "LineToShowPattern";
        stats = GameObject.Find("Stats").GetComponent<UILabel>();
        currentPositions = new List<Vector3>();
        lines = new List<LineRenderer>();
        timerLabel = GameObject.Find("TimerLabel").GetComponent<UILabel>();
        timeLabel = GameObject.Find("TimeLabel").GetComponent<UILabel>();
        rand = new System.Random();
        currentPointsToRecognize = new List<Point>();
        scoreLabel = GameObject.Find("ScoreLabel").GetComponent<UILabel>();
        linesPositions = new List<List<Vector3>>();
        totalDevider = 1.0f;
        restartButton = GameObject.Find("RestartButton");
        addButton = GameObject.Find("AddButton").GetComponent<UIButton>();
        patternNameInput = GameObject.Find("PatternNameInput").GetComponent<UIInput>();
        escLabel = GameObject.Find("ESCLabel").GetComponent<UILabel>();
        positionsForPatternAddition = new List<Vector3>();
        pointsForPatternAddition = new List<Point>();
        mainMenuParrent = GameObject.Find("MainMenuParrent");
        inGamParrent = GameObject.Find("InGameParrent"); ;
        inPattenrAdditionParrent = GameObject.Find("InPatternAdditionParrent");
    }
    void ShowLineHandler(object sender, EventArgs args)
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            List<Vector3> tempLine = new List<Vector3>();
            foreach (var point in gestureLibrary[0].Points)
            {
                //tempLine.Add(Camera.main.ScreenToWorldPoint(new Vector3(point.X, -point.Y, 1f)));
                tempLine.Add(new Vector3(point.X, -point.Y, 1));
                //Debug.Log(tempLine[tempLine.Count - 1].ToString());
                //Debug.Log(point.X + " " + point.Y);
            }
            linesample.SetVertexCount(tempLine.Count);
            linesample.SetPositions(tempLine.ToArray());
            updateEvent -= ShowLineHandler;
            Debug.Log("After ShowLineHandler");
        }
    }
    void QuitHandler(object sender, EventArgs args)
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Bye bye!");
            Application.Quit();
        }
    }
    #endregion

    #region Invokers
     void StartButonPressEventInvoker()
    {
        if (startButtonPressEvent != null)
        {
            startButtonPressEvent(null, null);
        }
    }
     void AddNewPatternButonPressEventInvoker()
    {
        if (addNewPatternButtonPressEvent != null)
        {
            addNewPatternButtonPressEvent(null, null);
        }
    }
    #endregion

    #region Other

    public RecognitionGame()
    {
        startEvent += FieldsDefinitionHandler;
        startEvent += LoadPatternsHandler;
        startEvent += LoadCustomPatternsHandler;
        startButtonPressEvent += StartButtonPressHandler;
        addNewPatternButtonPressEvent += AddNewPatternPressButtonHandler;
        //updateEvent += QuiteHandler;
    }
    // Use this for initialization
    void Start()
    {
        if (startEvent != null)
        {
            startEvent(null, null);
        }
        BackToMainMenu();

    }
    // Update is called once per frame
    void Update()
    {
        if (updateEvent != null)
        {
            updateEvent(null, null);
        }
    }
    void ShowRandomPattern(object sender, EventArgs args)
    {
        List<Vector3> tempLine = new List<Vector3>();
        int randomIndex = rand.Next(0, gestureLibrary.Count);
        patternToRecognize = gestureLibrary[randomIndex].Name;
        foreach (var point in gestureLibrary[randomIndex].Points)
        {
            tempLine.Add(new Vector3(point.X, -point.Y, 1));
        }
        lineToShowPattern.SetVertexCount(tempLine.Count);
        lineToShowPattern.SetPositions(tempLine.ToArray());
        updateEvent -= TimeCheckHandler;
        updateEvent -= InGameDrawHnadler;
        StartCoroutine(Delay(patternShowTime));
    }
    void Flush()
    {
        foreach (var line in lines)
        {
            Destroy(line.gameObject);
        }
        if (lineForPatternAddition != null)
        {
            lineForPatternAddition.SetVertexCount(0);
        }
        pointsForPatternAddition.Clear();
        positionsForPatternAddition.Clear();
        lines.Clear();
        linesPositions.Clear();
        lineToShowPattern.SetVertexCount(0);
    }
    IEnumerator Delay(float delayTime)
    {
        timerLabel.text = "--.--";
        yield return new WaitForSeconds(delayTime);
        timeLabel.gameObject.SetActive(true);
        timerLabel.gameObject.SetActive(true);
        scoreLabel.gameObject.SetActive(true);
        lineToShowPattern.SetVertexCount(0);
        updateEvent += InGameDrawHnadler;
        updateEvent += TimeCheckHandler;
        currentTime = startTime / totalDevider;
    }
    IEnumerator Cleaner()
    {
        while (true)
        {
            for (int i = 0; i < linesPositions.Count; ++i)
            {
                if (linesPositions[i].Count > 1)
                {
                    linesPositions[i].RemoveAt(0);
                    lines[i].SetPositions(linesPositions[i].ToArray());
                }
                else if (currentPositions != linesPositions[i])
                {
                    Destroy(lines[i].gameObject);
                    lines.RemoveAt(i);
                    linesPositions.RemoveAt(i);
                }
            }
            yield return new WaitForSeconds(pointInLineLifeTime);
        }
    }
    void BackToMainMenu()
    {
        Flush();
        mainMenuParrent.SetActive(true);
        inGamParrent.SetActive(false);
        inPattenrAdditionParrent.SetActive(false);
        StopAllCoroutines();
        updateEvent = null;
        updateEvent += QuitHandler;
    }
    #endregion
}
