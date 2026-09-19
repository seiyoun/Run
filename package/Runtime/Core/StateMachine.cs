/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 汎用ステートマシンとステート処理のインターフェースを定義する。
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shiyuan.Foundation.Core
{
    public interface IState<TState> where TState : struct, Enum
    {
        TState State { get; }

        /// <summary>
        /// パラメータを受け取り、ステート開始時の処理を実行する。
        /// </summary>
        Task EnterAsync(object parameter, CancellationToken cancellationToken);

        /// <summary>
        /// 更新開始前に必要な待機処理を実行する。
        /// </summary>
        Task WaitAsync(CancellationToken cancellationToken);

        /// <summary>
        /// ステートの毎フレーム処理を実行する。
        /// </summary>
        void Update(float deltaTime);

        /// <summary>
        /// ステート終了時の処理を実行する。
        /// </summary>
        void Exit();
    }

    public sealed class StateMachine<TState> : IDisposable where TState : struct, Enum
    {
        private readonly Dictionary<TState, IState<TState>> states = new();
        private IState<TState> currentState;
        private TState? requestedState;
        private object requestedParameter;

        public TState CurrentState { get; private set; }
        public bool HasCurrentState { get; private set; }
        public bool IsChangingState { get; private set; }

        /// <summary>
        /// ステートマシンで使用するステートを登録する。
        /// </summary>
        public void AddState(IState<TState> state)
        {
            states[state.State] = state;
        }

        /// <summary>
        /// 登録済みステートを取得する。
        /// </summary>
        public IState<TState> GetState(TState state)
        {
            if (!states.TryGetValue(state, out var registeredState))
            {
                throw new InvalidOperationException($"State is not registered. state:{state}");
            }

            return registeredState;
        }

        /// <summary>
        /// 指定されたステートへ遷移する。遷移中の場合は最後の要求だけを予約する。
        /// </summary>
        public Task ChangeStateAsync(TState state, CancellationToken cancellationToken)
        {
            return ChangeStateAsync(state, null, cancellationToken);
        }

        /// <summary>
        /// パラメータを渡して指定されたステートへ遷移する。遷移中の場合は最後の要求だけを予約する。
        /// </summary>
        public async Task ChangeStateAsync(TState state, object parameter, CancellationToken cancellationToken)
        {
            if (IsChangingState)
            {
                requestedState = state;
                requestedParameter = parameter;
                return;
            }

            IsChangingState = true;

            try
            {
                var nextState = state;
                var nextParameter = parameter;
                while (true)
                {
                    requestedState = null;
                    requestedParameter = null;
                    await ExecuteStateChangeAsync(nextState, nextParameter, cancellationToken);

                    if (!requestedState.HasValue)
                    {
                        break;
                    }

                    nextState = requestedState.Value;
                    nextParameter = requestedParameter;
                }
            }
            finally
            {
                IsChangingState = false;
                requestedParameter = null;
            }
        }

        /// <summary>
        /// 現在ステートの毎フレーム処理を実行する。
        /// </summary>
        public void Update(float deltaTime)
        {
            if (IsChangingState)
            {
                return;
            }

            currentState?.Update(deltaTime);
        }

        /// <summary>
        /// 現在ステートを終了し、保持している状態を破棄する。
        /// </summary>
        public void Dispose()
        {
            currentState?.Exit();
            currentState = null;
            requestedState = null;
            requestedParameter = null;
            HasCurrentState = false;
        }

        /// <summary>
        /// 指定されたステートへの実際の遷移処理を実行する。
        /// </summary>
        private async Task ExecuteStateChangeAsync(TState state, object parameter, CancellationToken cancellationToken)
        {
            if (!states.TryGetValue(state, out var nextState))
            {
                throw new InvalidOperationException($"State is not registered. state:{state}");
            }

            currentState?.Exit();
            currentState = null;
            HasCurrentState = false;

            await nextState.EnterAsync(parameter, cancellationToken);
            if (requestedState.HasValue)
            {
                nextState.Exit();
                return;
            }

            await nextState.WaitAsync(cancellationToken);
            if (requestedState.HasValue)
            {
                nextState.Exit();
                return;
            }

            currentState = nextState;
            CurrentState = state;
            HasCurrentState = true;
        }
    }
}
