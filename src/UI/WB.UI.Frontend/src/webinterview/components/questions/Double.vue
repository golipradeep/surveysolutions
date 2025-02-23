<template>
    <wb-question :question="$me"
        questionCssClassName="numeric-question"
        :no-comments="noComments">
        <div class="question-unit">
            <div class="options-group">
                <div class="form-group">
                    <div class="field"
                        :class="{ answered: $me.isAnswered }">
                        <input
                            type="text"
                            autocomplete="off"
                            inputmode="decimal"
                            class="field-to-fill"
                            ref="inputDouble"
                            :placeholder="noAnswerWatermark"
                            :title="noAnswerWatermark"
                            :value="$me.answer"
                            v-blurOnEnterKey
                            @blur="answerDoubleQuestion"
                            :disabled="isSpecialValueSelected || !$me.acceptAnswer"
                            :class="{ 'special-value-selected': isSpecialValueSelected }"
                            v-numericFormatting="{minimumValue:'-99999999999999.99999999999999',
                                                  maximumValue:'99999999999999.99999999999999',
                                                  digitGroupSeparator: groupSeparator,
                                                  decimalCharacter:decimalSeparator,
                                                  decimalPlaces: decimalPlacesCount,
                                                  allowDecimalPadding: false}"/>
                        <wb-remove-answer v-if="!isSpecialValueSelected"
                            :on-remove="removeAnswer" />
                    </div>
                </div>
                <template v-if="isSpecialValueSelected != false">
                    <div
                        class="radio"
                        v-for="option in $me.options"
                        :key="$me.id + '_' + option.value">
                        <div class="field">
                            <input
                                class="wb-radio"
                                type="radio"
                                :id="$me.id + '_' + option.value"
                                :name="$me.id"
                                :value="option.value"
                                :disabled="!$me.acceptAnswer"
                                v-model="specialValue"/>
                            <label :for="$me.id + '_' + option.value">
                                <span class="tick"></span>
                                {{option.title}}
                            </label>
                            <wb-remove-answer :on-remove="removeAnswer" />
                        </div>
                    </div>
                </template>
                <wb-lock />
            </div>
        </div>
    </wb-question>
</template>
<script lang="js">
import { entityDetails } from '../mixins'
import * as $ from 'jquery'
import { getGroupSeparator, getDecimalSeparator, getDecimalPlacesCount } from './question_helpers'

export default {
    data() {
        return {
            autoNumericElement: null}
    },
    name: 'Double',
    props: ['noComments'],
    mixins: [entityDetails],
    computed: {
        isSpecialValueSelected(){
            if (this.$me.answer == null || this.$me.answer == undefined)
                return undefined
            return this.isSpecialValue(this.$me.answer)
        },
        noAnswerWatermark() {
            return !this.$me.acceptAnswer && !this.$me.isAnswered ? this.$t('Details.NoAnswer') : this.$t('WebInterviewUI.DecimalEnter')
        },
        groupSeparator() {
            return getGroupSeparator(this.$me)
        },
        decimalSeparator() {
            return getDecimalSeparator(this.$me)
        },
        decimalPlacesCount() {
            return getDecimalPlacesCount(this.$me)
        },
        specialValue: {
            get() {
                return this.$me.answer
            },
            set(value) {
                this.saveAnswer(value, true)
            },
        },
    },
    methods: {
        answerDoubleQuestion(evnt) {
            const answerString = this.autoNumericElement.getNumericString()
            if (answerString.replace(/[^0-9]/g, '').length > 15) {
                this.markAnswerAsNotSavedWithMessage(this.$t('WebInterviewUI.DecimalTooBig'))
                return
            }

            const answer = answerString != undefined && answerString != ''
                ? parseFloat(answerString)
                : null

            const isSpecialValue = this.isSpecialValue(answer)
            this.saveAnswer(answer, isSpecialValue)
        },
        saveAnswer(answer, isSpecialValue){
            this.sendAnswer(() => {
                if(this.handleEmptyAnswer(answer)) {
                    return
                }
                if (answer > 999999999999999 || answer < -999999999999999) {
                    this.markAnswerAsNotSavedWithMessage(this.$t('WebInterviewUI.DecimalCannotParse'))
                    return
                }

                this.$store.dispatch('answerDoubleQuestion', { identity: this.id, answer: answer })
            })
        },
        isSpecialValue(value){
            const options = this.$me.options || []
            if (options.length == 0)
                return false
            for(let i=0;i<options.length;i++)
            {
                if (options[i].value === value)
                    return true
            }
            return false
        },
        removeAnswer() {
            if(this.autoNumericElement)
                this.autoNumericElement.clear()

            this.$store.dispatch('removeAnswer', this.id)
            return
        },
    },
    beforeDestroy () {
        if (this.autoNumericElement) {
            this.autoNumericElement.remove()
        }
    },
}

</script>
