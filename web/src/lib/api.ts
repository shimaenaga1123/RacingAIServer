import {API_BASE, SIGNALR_URL} from "$app/env/public";

export interface CompetitionRules {
  tickRate: number;
  actionTimeoutMs: number;
  maxConsecutiveTimeouts: number;
  totalLaps: number;
  maxParticipants: number;
}

export interface Participant {
  id: string;
  name: string;
  registeredAt: string;
}

export interface CompetitionDetails {
  id: string;
  code: string;
  name: string;
  trackId: string;
  status: string;
  rules: CompetitionRules;
  participants: Participant[];
}

export interface LeaderboardItem {
  rank: number;
  participantId: string;
  participantName: string;
  totalPoints: number;
  racesParticipated: number;
  wins: number;
  podiums: number;
  bestLapTimeSeconds: number;
}

export interface JoinCompetitionResponse {
  competitionId: string;
  participantId: string;
  agentName: string;
  sessionToken: string;
}

async function parseError(response: Response, fallback: string): Promise<string> {
  const body = await response.json().catch(() => null);

  if (body && typeof body.error === "string") {
    return body.error;
  }

  return fallback;
}

export async function createCompetition(data: {
  name: string;
  trackId?: string;
  totalLaps?: number;
  maxParticipants?: number;
}): Promise<CompetitionDetails> {
  const res = await fetch(`${API_BASE}/competitions`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(data),
  });

  if (!res.ok) {
    throw new Error(
      await parseError(res, "대회를 생성하지 못했습니다."),
    );
  }

  const result = await res.json();

  return {
    ...result,
    participants: result.participants ?? [],
  };
}

export async function getCompetition(
  code: string,
): Promise<CompetitionDetails> {
  const res = await fetch(
    `${API_BASE}/competitions/${encodeURIComponent(code)}`,
  );

  if (!res.ok) {
    throw new Error(
      `대회 코드 ${code}를 찾을 수 없습니다.`,
    );
  }

  const result = await res.json();

  return {
    ...result,
    participants: result.participants ?? [],
  };
}

export async function joinCompetition(
  code: string,
  data: {
    agentName: string;
    sdkVersion: string;
  },
): Promise<JoinCompetitionResponse> {
  const res = await fetch(
    `${API_BASE}/competitions/${encodeURIComponent(code)}/join`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(data),
    },
  );

  if (!res.ok) {
    throw new Error(
      await parseError(res, "대회 참가에 실패했습니다."),
    );
  }

  return res.json();
}

export async function startCompetition(
  code: string,
): Promise<{ status: string }> {
  const res = await fetch(
    `${API_BASE}/competitions/${encodeURIComponent(code)}/start`,
    {
      method: "POST",
    },
  );

  if (!res.ok) {
    throw new Error(
      await parseError(res, "대회를 시작하지 못했습니다."),
    );
  }

  return res.json();
}

export async function getLeaderboard(
  code: string,
): Promise<LeaderboardItem[]> {
  const res = await fetch(
    `${API_BASE}/competitions/${encodeURIComponent(code)}/leaderboard`,
  );

  if (!res.ok) {
    return [];
  }

  return res.json();
}

export function competitionStatusLabel(status: string): string {
  switch (status.toUpperCase()) {
    case "CREATED":
      return "생성됨";
    case "OPEN":
      return "참가 모집 중";
    case "READY":
      return "출전 준비";
    case "RUNNING":
      return "레이스 진행 중";
    case "FINISHED":
      return "종료";
    default:
      return status;
  }
}

export function formatTime(seconds: number): string {
  if (!Number.isFinite(seconds) || seconds <= 0) {
    return "—";
  }

  const minutes = Math.floor(seconds / 60);
  const remaining = seconds - minutes * 60;

  return `${minutes}:${remaining.toFixed(3).padStart(6, "0")}`;
}
