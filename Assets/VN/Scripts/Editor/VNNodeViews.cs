#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace VN.Editor
{
    // Start Node
    public class VNStartNodeView : VNStoryNodeView
    {
        public VNStartNodeView() : base("Start Node", TriggerColor, VNStoryNodeType.Start)
        {
            CreateOutputPort("Out");
        }
    }

    // Wait Node
    public class VNWaitNodeView : VNStoryNodeView
    {
        private FloatField _durationField;
        private Toggle _allowSkipToggle;

        public VNWaitNodeView() : base("Wait Duration", TriggerColor, VNStoryNodeType.Wait)
        {
            CreateInputPort();
            CreateOutputPort();

            _durationField = new FloatField("Duration (sec)") { value = 1.0f };
            _allowSkipToggle = new Toggle("Allow Skip") { value = false };

            mainContainer.Add(_durationField);
            mainContainer.Add(_allowSkipToggle);
        }

        public override VNStoryNodeData ToData()
        {
            var d = base.ToData();
            d.waitDuration = _durationField.value;
            d.allowSkipAnimation = _allowSkipToggle.value;
            return d;
        }

        public override void LoadFromData(VNStoryNodeData data)
        {
            base.LoadFromData(data);
            _durationField.value = data.waitDuration;
            _allowSkipToggle.value = data.allowSkipAnimation;
        }
    }

    // Triggers
    public class VNTriggerNodeView : VNStoryNodeView
    {
        private TextField _keyField;

        public VNTriggerNodeView(string title, VNStoryNodeType type) : base(title, TriggerColor, type)
        {
            CreateOutputPort("Out");

            _keyField = new TextField("Trigger Key");
            mainContainer.Add(_keyField);
        }

        public override VNStoryNodeData ToData()
        {
            var d = base.ToData();
            d.triggerKey = _keyField.value;
            return d;
        }

        public override void LoadFromData(VNStoryNodeData data)
        {
            base.LoadFromData(data);
            _keyField.value = data.triggerKey;
        }
    }

    // Condition
    public class VNCheckFlagNodeView : VNStoryNodeView
    {
        private TextField _flagField;
        private Toggle _invertToggle;

        public VNCheckFlagNodeView() : base("Check Flag", ConditionColor, VNStoryNodeType.CheckFlag)
        {
            CreateInputPort();
            CreateOutputPort("True (Index 0)");
            CreateOutputPort("False (Index 1)");

            _flagField = new TextField("Flag Key");
            _invertToggle = new Toggle("Invert (NOT)");
            mainContainer.Add(_flagField);
            mainContainer.Add(_invertToggle);
        }

        public override VNStoryNodeData ToData()
        {
            var d = base.ToData();
            d.flagKey = _flagField.value;
            d.invertFlag = _invertToggle.value;
            return d;
        }

        public override void LoadFromData(VNStoryNodeData data)
        {
            base.LoadFromData(data);
            _flagField.value = data.flagKey;
            _invertToggle.value = data.invertFlag;
        }
    }

    // Character Actions
    public class VNSetCharacterSpriteNodeView : VNStoryNodeView
    {
        private EnumField _posField;
        private ObjectField _spriteField;
        private TextField _animFlagField;
        private TextField _spriteFlagField;
        private Toggle _waitToggle;
        private Toggle _allowSkipToggle;

        public VNSetCharacterSpriteNodeView() : base("Set Character Sprite", ActionColor, VNStoryNodeType.SetCharacterSprite)
        {
            CreateInputPort();
            CreateOutputPort();

            _posField = new EnumField("Position", VNCharacterPosition.Middle);
            _spriteField = new ObjectField("Sprite") { objectType = typeof(Sprite) };
            _animFlagField = new TextField("Anim Flag (e.g. slow_fade_in)");
            _spriteFlagField = new TextField("Sprite Flag");
            _waitToggle = new Toggle("Wait For Completion") { value = true };
            _allowSkipToggle = new Toggle("Allow Skip Animation") { value = false };

            mainContainer.Add(_posField);
            mainContainer.Add(_spriteField);
            mainContainer.Add(_animFlagField);
            mainContainer.Add(_spriteFlagField);
            mainContainer.Add(_waitToggle);
            mainContainer.Add(_allowSkipToggle);
        }

        public override VNStoryNodeData ToData()
        {
            var d = base.ToData();
            d.position = (VNCharacterPosition)_posField.value;
            d.characterSprite = (Sprite)_spriteField.value;
            d.animationFlag = _animFlagField.value;
            d.spriteFlag = _spriteFlagField.value;
            d.waitForCompletion = _waitToggle.value;
            d.allowSkipAnimation = _allowSkipToggle.value;
            return d;
        }

        public override void LoadFromData(VNStoryNodeData data)
        {
            base.LoadFromData(data);
            _posField.value = data.position;
            _spriteField.value = data.characterSprite;
            _animFlagField.value = data.animationFlag;
            _spriteFlagField.value = data.spriteFlag;
            _waitToggle.value = data.waitForCompletion;
            _allowSkipToggle.value = data.allowSkipAnimation;
        }
    }

    public class VNClearCharacterNodeView : VNStoryNodeView
    {
        private EnumField _posField;
        private TextField _animFlagField;
        private Toggle _waitToggle;
        private Toggle _allowSkipToggle;

        public VNClearCharacterNodeView() : base("Clear Character Position", ActionColor, VNStoryNodeType.ClearCharacter)
        {
            CreateInputPort();
            CreateOutputPort();

            _posField = new EnumField("Position", VNCharacterPosition.Middle);
            _animFlagField = new TextField("Anim Flag (e.g. slow_fade_out)");
            _waitToggle = new Toggle("Wait For Completion") { value = true };
            _allowSkipToggle = new Toggle("Allow Skip Animation") { value = false };

            mainContainer.Add(_posField);
            mainContainer.Add(_animFlagField);
            mainContainer.Add(_waitToggle);
            mainContainer.Add(_allowSkipToggle);
        }

        public override VNStoryNodeData ToData()
        {
            var d = base.ToData();
            d.position = (VNCharacterPosition)_posField.value;
            d.animationFlag = _animFlagField.value;
            d.waitForCompletion = _waitToggle.value;
            d.allowSkipAnimation = _allowSkipToggle.value;
            return d;
        }

        public override void LoadFromData(VNStoryNodeData data)
        {
            base.LoadFromData(data);
            _posField.value = data.position;
            _animFlagField.value = data.animationFlag;
            _waitToggle.value = data.waitForCompletion;
            _allowSkipToggle.value = data.allowSkipAnimation;
        }
    }

    // Background Action
    public class VNSetBackgroundNodeView : VNStoryNodeView
    {
        private ObjectField _bgField;
        private TextField _animFlagField;
        private Toggle _waitToggle;
        private Toggle _allowSkipToggle;

        public VNSetBackgroundNodeView() : base("Set Background", ActionColor, VNStoryNodeType.SetBackground)
        {
            CreateInputPort();
            CreateOutputPort();

            _bgField = new ObjectField("Background") { objectType = typeof(Sprite) };
            _animFlagField = new TextField("Anim Flag (e.g. slow_fade_in)");
            _waitToggle = new Toggle("Wait For Completion") { value = true };
            _allowSkipToggle = new Toggle("Allow Skip Animation") { value = false };

            mainContainer.Add(_bgField);
            mainContainer.Add(_animFlagField);
            mainContainer.Add(_waitToggle);
            mainContainer.Add(_allowSkipToggle);
        }

        public override VNStoryNodeData ToData()
        {
            var d = base.ToData();
            d.backgroundSprite = (Sprite)_bgField.value;
            d.backgroundAnimFlag = _animFlagField.value;
            d.waitForCompletion = _waitToggle.value;
            d.allowSkipAnimation = _allowSkipToggle.value;
            return d;
        }

        public override void LoadFromData(VNStoryNodeData data)
        {
            base.LoadFromData(data);
            _bgField.value = data.backgroundSprite;
            _animFlagField.value = data.backgroundAnimFlag;
            _waitToggle.value = data.waitForCompletion;
            _allowSkipToggle.value = data.allowSkipAnimation;
        }
    }

    // Audio Nodes
    public class VNPlayBGMNodeView : VNStoryNodeView
    {
        private ObjectField _clipField;

        public VNPlayBGMNodeView() : base("Play BGM", ActionColor, VNStoryNodeType.PlayBGM)
        {
            CreateInputPort();
            CreateOutputPort();

            _clipField = new ObjectField("BGM Clip") { objectType = typeof(AudioClip) };
            mainContainer.Add(_clipField);
        }

        public override VNStoryNodeData ToData()
        {
            var d = base.ToData();
            d.audioClip = (AudioClip)_clipField.value;
            return d;
        }

        public override void LoadFromData(VNStoryNodeData data)
        {
            base.LoadFromData(data);
            _clipField.value = data.audioClip;
        }
    }

    public class VNPlaySFXNodeView : VNStoryNodeView
    {
        private ObjectField _clipField;

        public VNPlaySFXNodeView() : base("Play SFX", ActionColor, VNStoryNodeType.PlaySFX)
        {
            CreateInputPort();
            CreateOutputPort();

            _clipField = new ObjectField("SFX Clip") { objectType = typeof(AudioClip) };
            mainContainer.Add(_clipField);
        }

        public override VNStoryNodeData ToData()
        {
            var d = base.ToData();
            d.audioClip = (AudioClip)_clipField.value;
            return d;
        }

        public override void LoadFromData(VNStoryNodeData data)
        {
            base.LoadFromData(data);
            _clipField.value = data.audioClip;
        }
    }

    // Flag & Event Actions
    public class VNSetFlagNodeView : VNStoryNodeView
    {
        private TextField _flagField;
        private Toggle _removeToggle;

        public VNSetFlagNodeView() : base("Set / Remove Flag", ActionColor, VNStoryNodeType.SetFlag)
        {
            CreateInputPort();
            CreateOutputPort();

            _flagField = new TextField("Flag Key");
            _removeToggle = new Toggle("Remove Flag");
            mainContainer.Add(_flagField);
            mainContainer.Add(_removeToggle);
        }

        public override VNStoryNodeData ToData()
        {
            var d = base.ToData();
            d.setFlagKey = _flagField.value;
            d.removeFlag = _removeToggle.value;
            return d;
        }

        public override void LoadFromData(VNStoryNodeData data)
        {
            base.LoadFromData(data);
            _flagField.value = data.setFlagKey;
            _removeToggle.value = data.removeFlag;
        }
    }

    public class VNFireEventNodeView : VNStoryNodeView
    {
        private TextField _eventField;

        public VNFireEventNodeView() : base("Fire Event", ActionColor, VNStoryNodeType.FireEvent)
        {
            CreateInputPort();
            CreateOutputPort();

            _eventField = new TextField("Event Key");
            mainContainer.Add(_eventField);
        }

        public override VNStoryNodeData ToData()
        {
            var d = base.ToData();
            d.eventKey = _eventField.value;
            return d;
        }

        public override void LoadFromData(VNStoryNodeData data)
        {
            base.LoadFromData(data);
            _eventField.value = data.eventKey;
        }
    }

    // Dialogue Node
    public class VNPlayDialogueNodeView : VNStoryNodeView
    {
        private TextField _speakerField;
        private TextField _dialogueField;
        private ObjectField _audioField;

        public VNPlayDialogueNodeView() : base("Play Dialogue", DialogueColor, VNStoryNodeType.PlayDialogue)
        {
            CreateInputPort();
            CreateOutputPort();

            _speakerField = new TextField("Speaker");
            _dialogueField = new TextField("Dialogue") { multiline = true };
            _dialogueField.style.height = 60;
            _audioField = new ObjectField("Audio Cue") { objectType = typeof(AudioClip) };

            mainContainer.Add(_speakerField);
            mainContainer.Add(_dialogueField);
            mainContainer.Add(_audioField);
        }

        public override VNStoryNodeData ToData()
        {
            var d = base.ToData();
            d.speakerName = _speakerField.value;
            d.dialogueText = _dialogueField.value;
            d.dialogueAudio = (AudioClip)_audioField.value;
            return d;
        }

        public override void LoadFromData(VNStoryNodeData data)
        {
            base.LoadFromData(data);
            _speakerField.value = data.speakerName;
            _dialogueField.value = data.dialogueText;
            _audioField.value = data.dialogueAudio;
        }
    }

    // Choices Node
    public class VNPlayChoicesNodeView : VNStoryNodeView
    {
        private VisualElement _choicesContainer;
        private List<TextField> _choiceInputs = new List<TextField>();

        public VNPlayChoicesNodeView() : base("Play Choices", DialogueColor, VNStoryNodeType.PlayChoices)
        {
            CreateInputPort();

            _choicesContainer = new VisualElement();
            mainContainer.Add(_choicesContainer);

            var addBtn = new Button(() => AddChoiceOption()) { text = "+ Add Choice Option" };
            mainContainer.Add(addBtn);

            AddChoiceOption();
            AddChoiceOption();
        }

        public void AddChoiceOption(string text = "")
        {
            int index = _choiceInputs.Count;
            var choiceField = new TextField($"Choice {index + 1}") { value = string.IsNullOrEmpty(text) ? $"Option {index + 1}" : text };
            _choiceInputs.Add(choiceField);
            _choicesContainer.Add(choiceField);

            CreateOutputPort($"Choice {index + 1} (Index {index})");
        }

        public override VNStoryNodeData ToData()
        {
            var d = base.ToData();
            d.choices.Clear();
            for (int i = 0; i < _choiceInputs.Count; i++)
            {
                d.choices.Add(new VNChoiceOption
                {
                    choiceText = _choiceInputs[i].value
                });
            }
            return d;
        }

        public override void LoadFromData(VNStoryNodeData data)
        {
            base.LoadFromData(data);
            _choicesContainer.Clear();
            _choiceInputs.Clear();
            outputContainer.Clear();

            if (data.choices != null && data.choices.Count > 0)
            {
                foreach (var choice in data.choices)
                {
                    AddChoiceOption(choice.choiceText);
                }
            }
            else
            {
                AddChoiceOption();
                AddChoiceOption();
            }
        }
    }
}
#endif
