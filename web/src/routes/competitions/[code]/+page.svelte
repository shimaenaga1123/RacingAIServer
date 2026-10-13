<script lang="ts">
  import { onMount } from "svelte";
  import * as signalR from "@microsoft/signalr";
  import { page } from "$app/state";

  import {
    competitionStatusLabel,
    formatTime,
    getCompetition,
    getLeaderboard,
    startCompetition,
    type CompetitionDetails,
    type LeaderboardItem,
  } from "#lib/api";

  import {
    createSpectatorConnection,
    startSpectatorConnection,
    stopSpectatorConnection,
    type CarSnapshot,
    type RaceSnapshot,
  } from "#lib/signalr";

  import {
    CanvasRenderer,
    DEFAULT_TRACK_POINTS,
  } from "#lib/renderer";

  const code = $derived(page.params.code);

  let competition = $state<CompetitionDetails | null>(null);
  let leaderboard = $state<LeaderboardItem[]>([]);
  let snapshot = $state<RaceSnapshot | null>(null);

  let loading = $state(true);
  let refreshing = $state(false);
  let starting = $state(false);

  let error = $state("");
  let copied = $state(false);

  let connectionState = $state(
    signalR.HubConnectionState.Disconnected,
  );

  let canvas = $state<HTMLCanvasElement | null>(null);
  let renderer = $state<CanvasRenderer | null>(null);
  let connection: signalR.HubConnection | null = null;

  let followParticipantId = $state<string | null>(null);
  let autoFollowLeader = $state(true);

  let resizeObserver: ResizeObserver | null = null;

  const trackPoints = DEFAULT_TRACK_POINTS;

  const isRunning = $derived(
    competition?.status?.toUpperCase() === "RUNNING",
  );

  const isFinished = $derived(
    competition?.status?.toUpperCase() === "FINISHED",
  );

  const participantCount = $derived(
    competition?.participants?.length ?? 0,
  );

  const visibleCars = $derived(snapshot?.cars ?? []);

  const leadingCar = $derived.by<CarSnapshot | null>(() => {
    if (!snapshot?.cars.length) {
      return null;
    }

    return [...snapshot.cars].sort(
      (a, b) =>
        b.distanceAlongTrack - a.distanceAlongTrack,
    )[0];
  });

  async function loadData() {
    if (!code) {
      return;
    }

    error = "";

    try {
      const [competitionData, leaderboardData] =
        await Promise.all([
          getCompetition(code),
          getLeaderboard(code),
        ]);

      competition = competitionData;
      leaderboard = leaderboardData;
    } catch (err) {
      error =
        err instanceof Error
          ? err.message
          : "대회 정보를 불러오지 못했습니다.";
    } finally {
      loading = false;
    }
  }

  async function refreshData() {
    if (!code) {
      return;
    }

    refreshing = true;

    try {
      const [competitionData, leaderboardData] =
        await Promise.all([
          getCompetition(code),
          getLeaderboard(code),
        ]);

      competition = competitionData;
      leaderboard = leaderboardData;
    } catch (err) {
      error =
        err instanceof Error
          ? err.message
          : "대회 정보를 새로고침하지 못했습니다.";
    } finally {
      refreshing = false;
    }
  }

  async function handleStartCompetition() {
    if (!code || starting) {
      return;
    }

    starting = true;
    error = "";

    try {
      await startCompetition(code);
      await refreshData();
    } catch (err) {
      error =
        err instanceof Error
          ? err.message
          : "대회를 시작하지 못했습니다.";
    } finally {
      starting = false;
    }
  }

  async function copyCode() {
    if (!competition) {
      return;
    }

    try {
      await navigator.clipboard.writeText(
        competition.code,
      );

      copied = true;

      window.setTimeout(() => {
        copied = false;
      }, 1600);
    } catch {
      copied = false;
    }
  }

  function renderSnapshot(next: RaceSnapshot) {
    snapshot = next;

    if (!renderer) {
      return;
    }

    let focusCar: CarSnapshot | undefined;

    if (followParticipantId) {
      focusCar = next.cars.find(
        (car) =>
          car.participantId === followParticipantId,
      );
    } else if (autoFollowLeader) {
      focusCar = [...next.cars].sort(
        (a, b) =>
          b.distanceAlongTrack -
          a.distanceAlongTrack,
      )[0];
    }

    if (focusCar) {
      renderer.focus(
        focusCar.x,
        focusCar.y,
      );
    }

    renderer.render(trackPoints, next.cars);
  }

  function selectParticipant(participantId: string) {
    followParticipantId = participantId;
    autoFollowLeader = false;

    const car = snapshot?.cars.find(
      (item) =>
        item.participantId === participantId,
    );

    if (car && renderer) {
      renderer.focus(car.x, car.y);
      renderer.render(trackPoints, snapshot?.cars ?? []);
    }
  }

  function showWholeTrack() {
    followParticipantId = null;
    autoFollowLeader = false;

    renderer?.fitToTrack(trackPoints);

    if (renderer) {
      renderer.render(
        trackPoints,
        snapshot?.cars ?? [],
      );
    }
  }

  function followLeader() {
    followParticipantId = null;
    autoFollowLeader = true;

    if (leadingCar && renderer) {
      renderer.focus(
        leadingCar.x,
        leadingCar.y,
      );

      renderer.render(
        trackPoints,
        snapshot?.cars ?? [],
      );
    }
  }

  function handleCanvasWheel(
    event: WheelEvent,
  ) {
    event.preventDefault();

    if (!renderer) {
      return;
    }

    renderer.zoomBy(
      event.deltaY > 0 ? -0.08 : 0.08,
    );

    renderer.render(
      trackPoints,
      snapshot?.cars ?? [],
    );
  }

  onMount(() => {
    if (!canvas) {
      return;
    }

    const context = canvas.getContext("2d");

    if (!context) {
      return;
    }

    renderer = new CanvasRenderer(context);

    const resize = () => {
      if (!canvas || !renderer) {
        return;
      }

      const rect = canvas.getBoundingClientRect();

      const dpr = Math.min(
        window.devicePixelRatio || 1,
        2,
      );

      canvas.width = Math.max(
        1,
        Math.floor(rect.width * dpr),
      );

      canvas.height = Math.max(
        1,
        Math.floor(rect.height * dpr),
      );

      context.setTransform(
        dpr,
        0,
        0,
        dpr,
        0,
        0,
      );

      renderer.resize(
        rect.width,
        rect.height,
      );

      renderer.fitToTrack(trackPoints);

      renderer.render(
        trackPoints,
        snapshot?.cars ?? [],
      );
    };

    resizeObserver = new ResizeObserver(resize);
    resizeObserver.observe(canvas);

    resize();

    void loadData();

    connection = createSpectatorConnection({
      onSnapshot: renderSnapshot,

      onRaceStarted: async () => {
        await refreshData();
      },

      onRaceFinished: async () => {
        await refreshData();
        await new Promise((resolve) =>
          window.setTimeout(resolve, 250),
        );
        await refreshData();
      },

      onConnectionStateChanged: (state) => {
        connectionState = state;
      },
    });

    void (async () => {
      try {
        await startSpectatorConnection(connection!);
      } catch (err) {
        console.error(
          "SignalR 연결 실패:",
          err,
        );
      }
    })();

    return () => {
      resizeObserver?.disconnect();
      resizeObserver = null;

      void stopSpectatorConnection(connection);

      connection = null;
      renderer = null;
    };
  });
</script>

<svelte:head>
  <title>
    {competition?.name ?? "대회"} — Racing AI
  </title>
</svelte:head>

<div class="container">
  {#if loading}
    <div style="padding: 100px 0; text-align: center;">
      <div class="status">
        <span class="status-dot"></span>
        대회 정보를 불러오는 중
      </div>
    </div>
  {:else if error && !competition}
    <div style="padding: 70px 0;">
      <div class="error-box">
        {error}
      </div>
    </div>
  {:else if competition}
    <section class="page-header">
      <div>
        <div
          style="
            display: flex;
            align-items: center;
            gap: 9px;
            flex-wrap: wrap;
            margin-bottom: 10px;
          "
        >
          <span
            class:running={isRunning}
            class:finished={isFinished}
            class:open={!isRunning && !isFinished}
            class="status"
          >
            <span class="status-dot"></span>
            {competitionStatusLabel(
              competition.status,
            )}
          </span>

          <div class="code-chip">
            CODE
            <span>{competition.code}</span>
          </div>
        </div>

        <h1 class="page-title">
          {competition.name}
        </h1>

        <p class="page-description">
          {competition.trackId}
          · {competition.rules.totalLaps}랩
          · 최대 {competition.rules.maxParticipants}명
        </p>
      </div>

      <div class="page-header-actions">
        <button
          class="ghost-button"
          type="button"
          onclick={copyCode}
        >
          {copied ? "복사됨" : "참가 코드 복사"}
        </button>

        <button
          class="secondary-button"
          type="button"
          disabled={refreshing}
          onclick={() => void refreshData()}
        >
          {refreshing ? "새로고침 중…" : "새로고침"}
        </button>

        {#if competition.status.toUpperCase() !== "RUNNING" && !isFinished}
          <button
            class="primary-button"
            type="button"
            disabled={starting}
            onclick={() =>
              void handleStartCompetition()}
          >
            {starting ? "시작 중…" : "대회 시작"}
          </button>
        {/if}
      </div>
    </section>

    {#if error}
      <div
        class="error-box"
        style="margin-bottom: 15px;"
      >
        {error}
      </div>
    {/if}

    <section class="stats-grid" style="margin-bottom: 15px;">
      <div class="stat">
        <div class="stat-label">Participants</div>
        <div class="stat-value">
          {participantCount}
          <span
            style="
              color: var(--text-muted);
              font-size: 13px;
              font-weight: 500;
            "
          >
            / {competition.rules.maxParticipants}
          </span>
        </div>
        <div class="stat-meta">현재 참가자</div>
      </div>

      <div class="stat">
        <div class="stat-label">Laps</div>
        <div class="stat-value">
          {competition.rules.totalLaps}
        </div>
        <div class="stat-meta">총 랩 수</div>
      </div>

      <div class="stat">
        <div class="stat-label">Tick Rate</div>
        <div class="stat-value">
          {competition.rules.tickRate}
          <span
            style="
              color: var(--text-muted);
              font-size: 12px;
              font-weight: 500;
            "
          >
            Hz
          </span>
        </div>
        <div class="stat-meta">simulation frequency</div>
      </div>

      <div class="stat">
        <div class="stat-label">Connection</div>
        <div class="stat-value" style="font-size: 15px;">
          {#if connectionState === signalR.HubConnectionState.Connected}
            LIVE
          {:else if connectionState === signalR.HubConnectionState.Reconnecting}
            RECONNECTING
          {:else}
            OFFLINE
          {/if}
        </div>
        <div class="stat-meta">
          SignalR spectator
        </div>
      </div>
    </section>

    <section class="dashboard-grid">
      <div class="panel viewer-panel">
        <div class="panel-header">
          <div>
            <h2 class="panel-title">
              실시간 레이스
            </h2>
            <p class="panel-subtitle">
              60 Hz simulation / 15 Hz spectator stream
            </p>
          </div>

          {#if snapshot}
            <div class="code-chip">
              TICK
              <span>
                {snapshot.tick.toLocaleString()}
              </span>
            </div>
          {/if}
        </div>

        <div class="viewer-wrap">
          <canvas
            bind:this={canvas}
            onwheel={handleCanvasWheel}
            aria-label="레이싱 실시간 렌더러"
          ></canvas>

          <div class="viewer-overlay">
            {#if snapshot}
              <div class="viewer-topbar">
                <div class="live-indicator">
                  <span class="live-dot"></span>
                  LIVE
                </div>

                <div class="code-chip">
                  {snapshot.cars.length} CARS
                </div>
              </div>

              <div class="viewer-bottom">
                <button
                  class="ghost-button"
                  type="button"
                  onclick={showWholeTrack}
                >
                  전체 보기
                </button>

                <button
                  class="ghost-button"
                  type="button"
                  onclick={followLeader}
                >
                  선두 따라가기
                </button>
              </div>
            {:else}
              <div class="viewer-empty">
                <div class="viewer-empty-card">
                  <div class="viewer-empty-icon">
                    ▷
                  </div>

                  <h3>
                    {isFinished
                      ? "레이스가 종료되었습니다"
                      : "레이스를 기다리는 중"}
                  </h3>

                  <p>
                    {isFinished
                      ? "최종 순위와 기록은 아래에서 확인할 수 있습니다."
                      : "대회가 시작되면 서버가 자동으로 레이스를 생성하고 실시간 경기를 전송합니다."}
                  </p>
                </div>
              </div>
            {/if}
          </div>
        </div>
      </div>

      <div class="side-stack">
        <div class="panel">
          <div class="panel-header">
            <div>
              <h2 class="panel-title">
                참가자
              </h2>
              <p class="panel-subtitle">
                {participantCount}명이 등록되어 있습니다.
              </p>
            </div>
          </div>

          <div class="panel-body">
            {#if competition.participants.length}
              <div class="participant-list">
                {#each competition.participants as participant, index}
                  <button
                    class="participant"
                    type="button"
                    onclick={() =>
                      selectParticipant(
                        participant.id,
                      )}
                    style="
                      appearance: none;
                      border: 0;
                      width: 100%;
                      text-align: left;
                      color: inherit;
                      background: transparent;
                    "
                  >
                    <span class="participant-index">
                      {String(index + 1).padStart(2, "0")}
                    </span>

                    <span class="participant-dot"></span>

                    <span class="participant-name">
                      {participant.name}
                    </span>

                    <span class="participant-meta">
                      {#if snapshot?.cars.some(
                        (car) =>
                          car.participantId ===
                          participant.id,
                      )}
                        LIVE
                      {:else}
                        READY
                      {/if}
                    </span>
                  </button>
                {/each}
              </div>
            {:else}
              <div class="empty">
                아직 참가자가 없습니다.
              </div>
            {/if}
          </div>
        </div>

        <div class="panel">
          <div class="panel-header">
            <div>
              <h2 class="panel-title">
                Python SDK
              </h2>
              <p class="panel-subtitle">
                참가 코드를 AI에 전달하세요.
              </p>
            </div>
          </div>

          <div class="panel-body">
            <div class="code-display">
              <div class="code-digits">
                {competition.code}
              </div>

              <button
                class="secondary-button"
                type="button"
                onclick={copyCode}
              >
                {copied ? "복사됨" : "복사"}
              </button>
            </div>

            <div
              class="info-box"
              style="margin-top: 14px;"
            >
              AI에서는
              <span class="mono">racing-sdk</span>의
              <span class="mono">client.join()</span>에
              이 코드를 사용합니다.
            </div>
          </div>
        </div>
      </div>
    </section>

    <section
      class="panel"
      style="margin-top: 15px;"
    >
      <div class="panel-header">
        <div>
          <h2 class="panel-title">
            순위표
          </h2>
          <p class="panel-subtitle">
            누적 대회 성적
          </p>
        </div>

        {#if snapshot?.raceId}
          <div class="code-chip">
            RACE
            <span>
              {snapshot.raceId.slice(0, 8)}
            </span>
          </div>
        {/if}
      </div>

      <div class="panel-body">
        {#if leaderboard.length}
          <div class="leaderboard">
            {#each leaderboard as entry}
              <div class="leaderboard-row">
                <div
                  class:top={entry.rank <= 3}
                  class="rank"
                >
                  #{entry.rank}
                </div>

                <div>
                  <div class="leaderboard-name">
                    {entry.participantName}
                  </div>

                  <div class="leaderboard-meta">
                    {entry.racesParticipated}경기
                    · {entry.wins}승
                    · {entry.podiums}회 입상
                  </div>
                </div>

                <div class="points">
                  {entry.totalPoints} pt
                </div>
              </div>
            {/each}
          </div>
        {:else}
          <div class="empty">
            아직 집계된 레이스가 없습니다.
          </div>
        {/if}
      </div>
    </section>

    {#if snapshot?.cars.length}
      <section
        class="panel"
        style="margin-top: 15px;"
      >
        <div class="panel-header">
          <div>
            <h2 class="panel-title">
              현재 레이스 텔레메트리
            </h2>
            <p class="panel-subtitle">
              실시간 simulation snapshot
            </p>
          </div>
        </div>

        <div class="panel-body">
          <div class="leaderboard">
            {#each snapshot.cars
              .slice()
              .sort(
                (a, b) =>
                  b.distanceAlongTrack -
                  a.distanceAlongTrack,
              ) as car, index}
              <div class="leaderboard-row">
                <div
                  class:top={index === 0}
                  class="rank"
                >
                  #{index + 1}
                </div>

                <div>
                  <div class="leaderboard-name">
                    {car.name}
                  </div>

                  <div class="leaderboard-meta">
                    Lap {car.lap}
                    · CP {car.nextCheckpoint}
                    ·
                    {Math.round(
                      car.speed * 3.6,
                    )}
                    km/h
                  </div>
                </div>

                <div class="points">
                  {car.lateralOffset.toFixed(1)} m
                </div>
              </div>
            {/each}
          </div>
        </div>
      </section>
    {/if}
  {/if}
</div>
