using System;
using System.Collections.Generic;
using Netcode.Variable;
using UnityEngine;

namespace Netcode.Components
{
    public class NetworkAnimator : NetworkBehaviour
    {

        private readonly Dictionary<int, NetworkVariable<bool>> _boolParameter =
            new Dictionary<int, NetworkVariable<bool>>();

        private readonly NetworkVariable<float> _speedVariable = new NetworkVariable<float>(0);

        private Animator _animator;
        private AnimatorControllerParameter[] _animatorControllerParameters;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        protected override void OnNetworkInit(List<INetworkVariable> networkVariables)
        {
            base.OnNetworkInit(networkVariables);
            _animatorControllerParameters = _animator.parameters;
            for (var i = 0; i < _animatorControllerParameters.Length; i++)
            {
                var parameter = _animatorControllerParameters[i];
                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Float:
                        break;
                    case AnimatorControllerParameterType.Int:
                        break;
                    case AnimatorControllerParameterType.Bool:
                    {
                        var variable = new NetworkVariable<bool>(parameter.defaultBool);
                        networkVariables.Add(variable);
                        _boolParameter[i] = variable;
                        break;
                    }
                    case AnimatorControllerParameterType.Trigger:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void Update()
        {
            if (IsClient)
            {
                foreach (var pair in _boolParameter)
                {
                    _animator.SetBool(_animatorControllerParameters[pair.Key].name, pair.Value.Value);
                }

                _animator.speed = _speedVariable.Value;
            }
            else
            {
                foreach (var pair in _boolParameter)
                {
                    pair.Value.Value = _animator.GetBool(_animatorControllerParameters[pair.Key].name);
                }

                _speedVariable.Value = _animator.speed;
            }
        }
    }
}