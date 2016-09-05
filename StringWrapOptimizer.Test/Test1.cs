// Test1.cs
// 
// Copyright (c) 2016-2016 midolin limegreen All right reserved.
// 
// License:
// See LICENSE.md or README.md in solution root directory.

using System;
using System.Linq;
using Xunit;

namespace KinokoBreaker.Test
{
    public class Test1
    {
        [Fact]
        public void アレ()
        {
            const string txt = "ゆかりさんのかわいさを堪能しましょう。それが私たちの生きる道です。わかりました？";
            Assert.Contains(Environment.NewLine, Model.Optimize(txt, fontSize: 64));
        }

        [Fact]
        public void アレ2()
        {
            // Wikipedia より
            const string txt =
                "風力発電（ふうりょくはつでん）とは風の力（風力）を利用した発電方式である。 風力エネルギーは再生可能エネルギーのひとつとして、自然環境の保全、エネルギーセキュリティの確保可能なエネルギー源として認められ、多くの地に風力発電所や風力発電装置が建設されている[1]。";
            Assert.Contains(Environment.NewLine, Model.Optimize(txt, fontSize: 64));
        }

        [Fact]
        public void アレ3()
        {
            // Wikipedia より
            const string txt =
                "Shinobu Omiya is a Japanese high school girl who, five years ago, had a homestay in England with a girl named Alice Cartelet. One day, Shinobu receives a letter from Alice saying she is coming to Japan to live with her. Surely enough, Alice appears and joins Shinobu and her friends Aya Komichi and Yoko Inokuma at her school, soon followed by Alice's friend from England, Karen Kujo.";
            Assert.Contains(Environment.NewLine, Model.Optimize(txt, fontSize: 64));
        }

        [Fact]
        public void アレ4()
        {
            // Wikipedia より
            const string txt =
                "イギリスでホームステイをしていた大宮忍に、帰国からしばらく経った高校1年生のある日1通のエアメールが届く。差出人はイギリスで出会った少女、アリス・カータレット。なんと今度はアリスが日本に来るという。アリスと忍、クラスメイトの日本少女、小路綾と猪熊陽子、さらにもう1人のイギリス少女、九条カレンも加わり、5人は金色に輝く日々を過ごしていく。";
            Assert.DoesNotContain('。',
                Model.Optimize(txt, fontSize: 64, breakPerElement: true).Split('\n').Select(i => i[0]));
        }
    }
}
