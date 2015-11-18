using UnityEngine;
using System;
using System.Collections;
using live2d;
using live2d.framework;

[ExecuteInEditMode]
public class DemoAndMotion : MonoBehaviour
{
	// ○○.moc.bytes 파일 어태치
	public TextAsset m_mocFile;
	// physics.json 파일 어태치
	public TextAsset m_physicsFile;
	// 텍스쳐 파일 어태치
	public Texture2D[] m_textureFiles;
	// ○○.mtn.bytes 파일 어태치
	public TextAsset[] m_motionFiles;
	
	// Live2D 모델를 .moc파일로부터 생성한다
	private Live2DModelUnity m_live2DModel;
	// 눈깜박임을 관리한다
	private EyeBlinkMotion m_eyeBlink = new EyeBlinkMotion();
	// 드래그 관리용
	private L2DTargetPoint m_dragMgr = new L2DTargetPoint();
	// 물리연산 관리
	private L2DPhysics m_physics;
	// 캔버스 관리용 변수
	private Matrix4x4 m_live2DCanvasPos;
	// 모션 여러 개를 저장하는 변수
	private Live2DMotion[] m_motions;
	// 모션 관리용 매니져
	private MotionQueueManager m_motionMgr;
	
	void Start () 
	{
		Live2D.init(); // Live2D 초기화
		load(); // 아래의 Load() 함수 실행
	}
	
	void load()
	{
		// .moc.bytes 파일을 불러와서 설정한다
		m_live2DModel = Live2DModelUnity.loadModel(m_mocFile.bytes);
		
		// 텍스쳐 파일 수만큼 읽어들여 설정한다
		for (int i = 0; i < m_textureFiles.Length; i++)
		{
			m_live2DModel.setTexture(i, m_textureFiles[i]);
		}
		
		// 캔버스 준비
		float modelWidth = m_live2DModel.getCanvasWidth();
		m_live2DCanvasPos = Matrix4x4.Ortho(0, modelWidth, modelWidth, 0, -50.0f, 50.0f);
		
		// 물리 설정 파일이 비어 있으면 불러온다
		if (m_physicsFile != null) m_physics = L2DPhysics.load(m_physicsFile.bytes);
		
		// 모션 관리용 변수를 준비한다
		m_motionMgr = new MotionQueueManager();
		
		// 메션 파일 수만큼 모션 관리용 배열을 확보한다
		m_motions = new Live2DMotion[m_motionFiles.Length];
		// 모션 파일 수만큼 모션을 읽어들인다
		for (int i = 0; i < m_motionFiles.Length; i++)
		{
			m_motions[i] = Live2DMotion.loadMotion(m_motionFiles[i].bytes);
		}
	}
	
	
	void Update()
	{
		// 마우스 커서 좌표 가져오기
		var pos = Input.mousePosition;
		if (Input.GetMouseButtonDown(0))
		{
			// 마우스 왼쪽 버튼이 눌린 순간에 실행할 처리를 여기에 기술한다
		}
		// 눌린 순간은 아니지만 마우스 왼쪽 버튼이 눌린 상태(드래그 중)
		else if (Input.GetMouseButton(0))
		{
			// 계산한 좌표를 드래그 관리용 변수에 넣는다
			m_dragMgr.Set(pos.x / Screen.width*2-1, pos.y/Screen.height*2-1);
		}
		// 마우스 왼쪽 버튼이 눌린 순간
		else if (Input.GetMouseButtonUp(0))
		{
			// 드래그 관리용 변수에 (0,0)을 넣는다
			m_dragMgr.Set(0, 0);
		}
		
		// 임의의 모션을 Z키로 재생한다
		if (Input.GetKeyDown (KeyCode.Z)) 
		{
			m_motionMgr.startMotion(m_motions[2]);
		}
	}
	
	// 렌더링할 때 호출된다
	void OnRenderObject()
	{
		// Live2D 모델이 없으면 읽어들인다
		if (m_live2DModel == null)
		{
			load();
		}
		
		m_live2DModel.setMatrix(transform.localToWorldMatrix * m_live2DCanvasPos);
		
		// 만일 애플리케이션이 동작 중이 아니라면 모델을 업데이트하여 렌더링하고 돌아온다
		if ( ! Application.isPlaying)
		{
			m_live2DModel.update();
			m_live2DModel.draw();
			return;
		}
		
		Idle (); // 아래에 있는 Idle() 함수 내용을 실행하고 여기로 돌아온다
		Drag (); // 아래에 있는 Drag() 함수 내용을 실행하고 여기로 돌아온다 ※이것은 Idle()을 실행한 수에 실행해야 한다
		
		// 눈깜박임 처리
		m_eyeBlink.setParam(m_live2DModel);
		
		// 물리 연산 업데이트
		if (m_physics != null) m_physics.updateParam(m_live2DModel);
		
		// 모델 업데이트
		m_live2DModel.update();
		
		// 모델 렌더링
		m_live2DModel.draw();
	}
	
	// 아무것도 하지 않을 때에 실행할 모션은 무작위로 재생한다
	void Idle ()
	{
		// 현재의 모션이 끝난 상태라면
		if (m_motionMgr.isFinished())
		{
			// 난수를 생성한다
			int rnd = UnityEngine.Random.Range(0, m_motions.Length - 1);
			// 모션 재생
			m_motionMgr.startMotion(m_motions[rnd]);
		}
		m_motionMgr.updateParam(m_live2DModel);
	}
	
	// 드래그한 결과를 Live2D 파라미터에 반영한다
	void Drag ()
	{
		m_dragMgr.update();
		// 얼굴이 향한 방향을 따라가는 처리
		m_live2DModel.addToParamFloat("PARAM_ANGLE_X", m_dragMgr.getX() * 30);
		m_live2DModel.addToParamFloat("PARAM_ANGLE_Y", m_dragMgr.getY() * 30);
		
		// 몸이 향한 방향을 따라가는 처리
		m_live2DModel.addToParamFloat("PARAM_BODY_ANGLE_X", m_dragMgr.getX() * 10);
		
		// 눈이 따라가게 하는 처리
		m_live2DModel.addToParamFloat("PARAM_EYE_BALL_X", m_dragMgr.getX());
		m_live2DModel.addToParamFloat("PARAM_EYE_BALL_Y", m_dragMgr.getY());
		
		// 시간에 따라 변화하는 사인파 곡선에 맞춰 호흡 파라미터를 업데이트한다
		double timeSec = UtSystem.getUserTimeMSec() / 1000.0;
		double t = timeSec * 2 * Math.PI;
		m_live2DModel.setParamFloat("PARAM_BREATH", (float)(0.5f + 0.5f * Math.Sin(t / 3.0)));
	}
}