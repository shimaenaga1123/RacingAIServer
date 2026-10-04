from __future__ import annotations
import urllib.request
import json
from .session import RacingSession


class RacingClient:
    def __init__(self, server_url: str):
        self._server_url = server_url.rstrip("/")

    def join(self, code: str, agent_name: str) -> RacingSession:
        """
        Join a competition using a 6-digit code or directly connect with token.
        """
        api_url = f"{self._server_url}/api/v1/competitions/{code}/join"
        payload = json.dumps({"agentName": agent_name, "sdkVersion": "0.1.0"}).encode("utf-8")
        req = urllib.request.Request(api_url, data=payload, headers={"Content-Type": "application/json"})

        session_token = f"rse_{code}"  # Fallback for direct testing
        try:
            with urllib.request.urlopen(req, timeout=5) as response:
                if response.status in (200, 201):
                    data = json.loads(response.read().decode("utf-8"))
                    session_token = data.get("sessionToken", session_token)
        except Exception:
            # REST 서버가 아직 안 떴거나 로컬 모드일 때 fallback 세션 사용
            pass

        return RacingSession(
            server_url=self._server_url,
            session_token=session_token,
            agent_name=agent_name,
            insecure=not self._server_url.startswith("https"),
        )
