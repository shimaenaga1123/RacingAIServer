<script lang="ts">
  import { goto } from "$app/navigation";
  import {
    createCompetition,
    getCompetition,
    type CompetitionDetails,
  } from "#lib/api";

  let joinCode = $state("");
  let competitionName = $state("");
  let trackId = $state("beginner-circuit");
  let totalLaps = $state(3);
  let maxParticipants = $state(8);

  let joining = $state(false);
  let creating = $state(false);
  let error = $state("");

  let createdCompetition = $state<CompetitionDetails | null>(null);

  function normalizeCode(value: string): string {
    return value
      .replace(/\D/g, "")
      .slice(0, 6);
  }

  async function handleOpenCompetition() {
    error = "";

    const code = normalizeCode(joinCode);

    if (code.length !== 6) {
      error = "6자리 대회 코드를 입력하세요.";
      return;
    }

    joining = true;

    try {
      await getCompetition(code);
      await goto(`/competitions/${code}`);
    } catch (err) {
      error =
        err instanceof Error
          ? err.message
          : "대회를 찾지 못했습니다.";
    } finally {
      joining = false;
    }
  }

  async function handleCreateCompetition() {
    error = "";
    createdCompetition = null;

    const name = competitionName.trim();

    if (!name) {
      error = "대회 이름을 입력하세요.";
      return;
    }

    creating = true;

    try {
      const competition = await createCompetition({
        name,
        trackId,
        totalLaps,
        maxParticipants,
      });

      createdCompetition = competition;

      await goto(
        `/competitions/${competition.code}`,
      );
    } catch (err) {
      error =
        err instanceof Error
          ? err.message
          : "대회를 생성하지 못했습니다.";
    } finally {
      creating = false;
    }
  }
</script>

<svelte:head>
  <title>Racing AI — AI 카레이싱 플랫폼</title>
</svelte:head>

<div class="container">
  <section
    id="how-it-works"
    class="dashboard-grid"
    style="margin-bottom: 15px;"
  >
    <div class="panel">
      <div class="panel-header">
        <div>
          <h2 class="panel-title">대회 참가</h2>
          <p class="panel-subtitle">
            6자리 코드만 있으면 바로 대회를 볼 수 있습니다.
          </p>
        </div>
      </div>

      <div class="panel-body">
        <form
          class="form-grid"
          onsubmit={(event) => {
            event.preventDefault();
            void handleOpenCompetition();
          }}
        >
          <div class="field">
            <label
              class="field-label"
              for="join-code"
            >
              대회 코드
            </label>

            <input
              id="join-code"
              inputmode="numeric"
              autocomplete="one-time-code"
              maxlength="6"
              placeholder="482731"
              bind:value={joinCode}
              oninput={(event) => {
                joinCode = normalizeCode(
                  event.currentTarget.value,
                );
              }}
            />
          </div>

          <div class="button-row">
            <button
              class="primary-button"
              type="submit"
              disabled={joining}
            >
              {joining ? "확인 중…" : "대회 열기"}
            </button>

            <div class="info-box" style="flex: 1;">
              AI 참가 자체는 Python SDK에서
              같은 6자리 코드를 사용합니다.
            </div>
          </div>
        </form>
      </div>
    </div>

    <div class="panel">
      <div class="panel-header">
        <div>
          <h2 class="panel-title">새 대회 만들기</h2>
          <p class="panel-subtitle">
            로컬 서버에서 바로 대회를 생성합니다.
          </p>
        </div>
      </div>

      <div class="panel-body">
        <form
          class="form-grid"
          onsubmit={(event) => {
            event.preventDefault();
            void handleCreateCompetition();
          }}
        >
          <div class="field">
            <label
              class="field-label"
              for="competition-name"
            >
              대회 이름
            </label>

            <input
              id="competition-name"
              placeholder="AI Racing Cup #1"
              bind:value={competitionName}
            />
          </div>

          <div
            style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px;"
          >
            <div class="field">
              <label
                class="field-label"
                for="laps"
              >
                랩 수
              </label>

              <input
                id="laps"
                type="number"
                min="1"
                max="99"
                bind:value={totalLaps}
              />
            </div>

            <div class="field">
              <label
                class="field-label"
                for="participants"
              >
                최대 참가자
              </label>

              <input
                id="participants"
                type="number"
                min="2"
                max="32"
                bind:value={maxParticipants}
              />
            </div>
          </div>

          <button
            class="primary-button"
            type="submit"
            disabled={creating}
          >
            {creating ? "생성 중…" : "대회 생성"}
          </button>
        </form>
      </div>
    </div>
  </section>

  {#if error}
    <div class="error-box" style="margin-bottom: 15px;">
      {error}
    </div>
  {/if}
</div>
